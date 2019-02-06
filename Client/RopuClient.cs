using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Client.StateModel;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class RopuClient : IControllingFunctionPacketHandler
    {
        const ushort _port = 1000;

        readonly ServingNodeClient _servingNodeClient;
        readonly ProtocolSwitch _protocolSwitch;
        readonly IClientSettings _clientSettings;

        IPEndPoint _servingNodeEndpoint;

        RopuState _start;
        RopuState _registered;
        RopuState _unregistered;
        RopuState _deregistering;
        RopuState _startingCall;
        RopuState _callInProgress;
        StateManager<StateId, EventId> _stateManager;
        LoadBalancerProtocol _loadBalancerProtocol;

        readonly Ropu.Shared.Timer _retryTimer;
        readonly IPAddress _ipAddress;
        readonly IPEndPoint _loadBalancerEndPoint;
        uint _registeredUserId = 0;

        public event EventHandler<EventArgs> StateChanged;
        /// <summary>
        /// this is the group the user has selected, it is the group to be called when they PTT and 
        /// the group to return to after the call
        /// </summary>
        ushort _idleGroup;
        /// <summary>
        /// The group of the current call, or the call we are trying to start
        /// </summary>
        ushort _callGroup;

        Task _heartbeatTask;
        CancellationTokenSource _heartbeatCancellationTokenSource = new CancellationTokenSource();
        ManualResetEvent _heartbeatOnEvent = new ManualResetEvent(false);

        public RopuClient(
            ProtocolSwitch protocolSwitch, 
            ServingNodeClient servingNodeClient, 
            IPAddress address,
            LoadBalancerProtocol loadBalancerProtocol,
            IPEndPoint loadBalancerEndPoint,
            IClientSettings clientSettings)
        {
            _clientSettings = clientSettings;
            _clientSettings.UserIdChanged += (sender, args) =>
            { 
                _stateManager.HandleEvent(EventId.UserIdChanged);
            };
            _loadBalancerEndPoint = loadBalancerEndPoint;
            _loadBalancerProtocol = loadBalancerProtocol;
            _protocolSwitch = protocolSwitch;
            _servingNodeClient = servingNodeClient;
            _servingNodeClient.SetControllingFunctionHandler(this);
            _retryTimer = new Ropu.Shared.Timer();

            //start
            _start = new RopuState(StateId.Start);
            _stateManager = new StateManager<StateId, EventId>(_start);
            _stateManager.AddState(_start);

            //registered
            _registered = new RopuState(StateId.Registered)
            {
                Entry = async token => 
                { 
                    _registeredUserId = _clientSettings.UserId;
                    if(_heartbeatTask == null)
                    {
                        _heartbeatTask = Heartbeat(_heartbeatCancellationTokenSource.Token);
                    }
                    _heartbeatOnEvent.Set(); //allows the heartbeat to continue
                    await Task.Run(() => {});
                },
            };
            _stateManager.AddState(_registered);
            _registered.AddTransition(EventId.CallRequest, () => _startingCall);
            _registered.AddTransition(EventId.CallStarted, () => _callInProgress);
            _registered.AddTransition(EventId.PttDown, () => _startingCall);

            //unregistered
            _unregistered = new RopuState(StateId.Unregistered)
            {
                Entry = async token => 
                {
                    _heartbeatOnEvent.Reset(); //stops the heartbeat
                    await Register(token);
                }
            };
            _unregistered.AddTransition(EventId.RegistrationResponseReceived, () => _registered);
            _stateManager.AddState(_unregistered);

            //deregistering
            _deregistering = new RopuState(StateId.Deregistering)
            {
                Entry = async token => await Deregister(token),
            };
            _deregistering.AddTransition(EventId.DeregistrationResponseReceived, () => _unregistered);
            _stateManager.AddState(_deregistering);

            //starting call
            _startingCall = new RopuState(StateId.StartingCall)
            {
                Entry = async token => await StartCall(token),
            };
            _startingCall.AddTransition(EventId.CallStartFailed, () => _registered);
            _startingCall.AddTransition(EventId.CallStarted, () => _callInProgress);
            _stateManager.AddState(_startingCall);

            //call in progress
            _callInProgress = new RopuState(StateId.CallInProgress);
            _stateManager.AddState(_callInProgress);

            _stateManager.StateChanged += (sender, args) => StateChanged?.Invoke(this, args);
            
            _stateManager.AddTransitionToAll(EventId.HeartbeatFailed, () => _unregistered, stateId => stateId != StateId.Unregistered);
            _stateManager.AddTransitionToAll(EventId.NotRegistered, () => _unregistered, stateId => stateId != StateId.Unregistered);
            _stateManager.AddTransitionToAll(EventId.UserIdChanged, () => _deregistering, stateId => stateId != StateId.Unregistered);
            _stateManager.AddTransitionToAll(EventId.CallEnded, () => _registered, stateId => stateId != StateId.Unregistered && stateId != StateId.Start && stateId != StateId.Deregistering);

            _ipAddress = address;
        }

        public StateId State
        {
            get => _stateManager.CurrentState.Identifier;
        }

        System.Timers.Timer CreateTimer(int interval, Action callback)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = interval;
            timer.AutoReset = false;
            timer.Elapsed += (sender, args) => callback();
            return timer;
        }

        public async Task Run()
        {
            var protocolSwitchTask = _protocolSwitch.Run();
            var loadBalancerTask = _loadBalancerProtocol.Run();
            _stateManager.SetState(_unregistered, _start);
            await TaskCordinator.WaitAll(protocolSwitchTask, loadBalancerTask);
        }

        readonly ManualResetEvent _heartbeatResetEvent = new ManualResetEvent(false);

        async Task<bool> WaitForEvent(CancellationToken token, ManualResetEvent resetEvent, int milliseconds)
        {
            return await Task.Run(() => 
            {
                var waitResult = WaitHandle.WaitAny(new []{token.WaitHandle, resetEvent}, milliseconds);
                return waitResult == 1;
            });
        }

        async Task WaitForEvent(ManualResetEvent resetEvent)
        {
            await Task.Run(() => 
            {
                resetEvent.WaitOne();
            });
        }

        async Task<bool> WaitForCancel(CancellationToken token, int milliseconds)
        {
            return await Task.Run(() => 
            {
                return token.WaitHandle.WaitOne(milliseconds);
            });
        }

        bool _heartbeatRequired = false;

        async Task Heartbeat(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                await WaitForEvent(_heartbeatOnEvent);
                await Task.Delay(25000, token);
                if(!_heartbeatRequired)
                {
                    continue;
                }
                _heartbeatResetEvent.Reset();
                bool heartbeatReceived = false;
                for(int attemptNumber = 0; attemptNumber < 3; attemptNumber++)
                {
                    Console.WriteLine("Sending Heartbeat");
                    _servingNodeClient.SendHeartbeat(_clientSettings.UserId, _servingNodeEndpoint);
                    heartbeatReceived = await WaitForEvent(token, _heartbeatResetEvent, 1000);
                    if(token.IsCancellationRequested) return;
                    if(heartbeatReceived)
                    {
                        break;
                    }
                }
                if(heartbeatReceived == false)
                {
                    Console.WriteLine("Heartbeat failed");
                    _stateManager.HandleEventNonBlocking(EventId.HeartbeatFailed);
                    return;
                }
            }
        }

        async Task Register(CancellationToken token)
        {
            Console.WriteLine("Register Called");
            while(!token.IsCancellationRequested)
            {
                if(_clientSettings.UserId == 0)
                {
                    if(await WaitForCancel(token, 2000))
                    {
                        return;
                    }
                    continue;
                }
                if(_servingNodeEndpoint == null)
                {
                    Console.WriteLine($"Requesting Serving Node from LoadBalancer {_loadBalancerEndPoint}");
                    _servingNodeEndpoint = await _loadBalancerProtocol.RequestServingNode(_loadBalancerEndPoint);
                    if(_servingNodeEndpoint == null)
                    {
                        Console.WriteLine("Failed to get a serving node");
                        if(await WaitForCancel(token, 2000)) 
                        {
                            return;
                        }
                        continue;
                    }
                }

                Console.WriteLine($"Got serving node at {_servingNodeEndpoint}");

                _servingNodeClient.Register(_clientSettings.UserId, _servingNodeEndpoint);
                if(await WaitForCancel(token, 2000))
                {
                    return;
                }
            }
        }

        async Task Deregister(CancellationToken token)
        {
            if(_registeredUserId == 0)
            {
                _stateManager.SetState(_unregistered, _deregistering);
                return;
            }
            
            while(!token.IsCancellationRequested)
            {
                _servingNodeClient.Deregister(_registeredUserId, _servingNodeEndpoint);
                await WaitForCancel(token, 2000);
            }
        }

        public void StartCall(ushort groupId)
        {
            _callGroup = groupId;
            _stateManager.HandleEvent(EventId.CallRequest);
        }

        async Task StartCall(CancellationToken token)
        {
            if(_callGroup == 0)
            {
                _callGroup = IdleGroup;
            }
            Console.WriteLine($"sending StartGroupCall for group {_callGroup} {_servingNodeEndpoint}");
            while(!token.IsCancellationRequested)
            {
                _servingNodeClient.StartGroupCall(_clientSettings.UserId, _callGroup, _servingNodeEndpoint);
                await WaitForCancel(token, 1000);
            }
        }

        public ushort IdleGroup
        {
            get => _idleGroup;
            set => _idleGroup = value;
        }

        public void PttUp()
        {

        }

        public void PttDown()
        {
            _stateManager.HandleEvent(EventId.PttDown);
        }

        void StartRetryTimer(int duration, Action callback)
        {
            _retryTimer.Duration = 1000;
            _retryTimer.Callback = callback;
            _retryTimer.Start();
        }

        public void HandleRegistrationResponseReceived(Codec codec, ushort bitrate)
        {
            _stateManager.HandleEvent(EventId.RegistrationResponseReceived);
        }

        public void HandleCallStarted(ushort groupId, uint userId)
        {
            _stateManager.HandleEvent(EventId.CallStarted);
        }

        public void HandleCallStartFailed(CallFailedReason reason)
        {
            Console.WriteLine($"CallStartFailed with reason {reason}");
            _stateManager.HandleEvent(EventId.CallStartFailed);
        }

        public void HandleHeartbeatResponseReceived()
        {
            _heartbeatResetEvent.Set();
        }

        public void HandleNotRegisteredReceived()
        {
            _stateManager.HandleEvent(EventId.NotRegistered);
        }

        public void HandleRegisterResponse()
        {
            _stateManager.HandleEvent(EventId.DeregistrationResponseReceived);
        }

        public void HandleCallEnded(ushort groupId)
        {
            _stateManager.HandleEvent(EventId.CallEnded);
        }
    }
}