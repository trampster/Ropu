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
        readonly MediaClient _mediaClient;

        readonly IClientSettings _clientSettings;

        RopuState _start;
        RopuState _registered;
        RopuState _unregistered;
        RopuState _deregistering;
        RopuState _startingCall;
        RopuState _inCallIdle;
        RopuState _inCallReceiveing;
        RopuState _inCallTransmitting;
        RopuState _inCallReleasingFloor;
        RopuState _inCallRequestingFloor;

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
            MediaClient mediaClient,
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
            _mediaClient = mediaClient;
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
                    _callGroup = IdleGroup;
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
                Exit = newState =>
                {
                    if(newState != _inCallTransmitting && newState != _inCallRequestingFloor)
                    {
                        _mediaClient.StopSendingAudio();
                    }
                }
            };
            _startingCall.AddTransition(EventId.CallStartFailed, () => _registered);
            _stateManager.AddState(_startingCall);

            //in call idle
            _inCallIdle = new RopuState(StateId.InCallIdle);
            _inCallIdle.AddTransition(EventId.PttDown, () => _inCallRequestingFloor);
            _stateManager.AddState(_inCallIdle);

            //in call receiving
            _inCallReceiveing = new RopuState(StateId.InCallReceiving);
            _stateManager.AddState(_inCallReceiveing);

            //in call transmitting
            _inCallTransmitting = new RopuState(StateId.InCallTransmitting)
            {
                Exit = newState =>
                {
                    if(newState != _inCallTransmitting && newState != _inCallRequestingFloor)
                    {
                        _mediaClient.StopSendingAudio();
                    }
                }
            };
            _inCallTransmitting.AddTransition(EventId.PttUp, () => _inCallReleasingFloor);
            _stateManager.AddState(_inCallTransmitting);

            //in call requesting floor
            _inCallRequestingFloor = new RopuState(StateId.InCallRequestingFloor)
            {
                Entry = token => 
                {
                    _servingNodeClient.SendFloorRequest(_callGroup, _clientSettings.UserId);
                    var ignore = _mediaClient.StartSendingAudio(_callGroup);
                    return new Task(() => {});
                },
                Exit = newState =>
                {
                    if(newState != _inCallTransmitting && newState != _inCallRequestingFloor)
                    {
                        _mediaClient.StopSendingAudio();
                    }
                }
            };
            _stateManager.AddState(_inCallRequestingFloor);

            //in call releasing floor
            _inCallReleasingFloor = new RopuState(StateId.InCallReleasingFloor)
            {
                Entry = token => 
                {
                    _servingNodeClient.SendFloorReleased(_callGroup, _clientSettings.UserId);
                    return new Task(() => {});
                }
            };
            _inCallReleasingFloor.AddTransition(EventId.FloorGranted, () => _inCallReleasingFloor);
            _stateManager.AddState(_inCallReleasingFloor);


            _inCallReleasingFloor.AddTransition(EventId.FloorGranted, () => _inCallReleasingFloor);


            _stateManager.StateChanged += (sender, args) => StateChanged?.Invoke(this, args);
            
            _stateManager.AddTransitionToAll(EventId.HeartbeatFailed, () => _unregistered, stateId => stateId != StateId.Unregistered);
            _stateManager.AddTransitionToAll(EventId.NotRegistered, () => _unregistered, stateId => stateId != StateId.Unregistered);
            _stateManager.AddTransitionToAll(EventId.UserIdChanged, () => _deregistering, stateId => stateId != StateId.Unregistered);
            _stateManager.AddTransitionToAll(EventId.CallEnded, () => _registered, stateId => stateId != StateId.Unregistered && stateId != StateId.Start && stateId != StateId.Deregistering);
            _stateManager.AddTransitionToAll(EventId.FloorIdle, () => _inCallIdle, stateId => true);
            _stateManager.AddTransitionToAll(EventId.FloorTaken, () => _inCallReceiveing, stateId => true);
            _stateManager.AddTransitionToAll(EventId.FloorGranted, () => _inCallTransmitting, stateId => stateId != StateId.InCallReleasingFloor);

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
            var playAudioTask = _mediaClient.PlayAudio();
            _stateManager.SetState(_unregistered, _start);
            await TaskCordinator.WaitAll(protocolSwitchTask, loadBalancerTask, playAudioTask);
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


        async Task Heartbeat(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                await WaitForEvent(_heartbeatOnEvent);
                await Task.Delay(25000, token);
                _heartbeatResetEvent.Reset();
                bool heartbeatReceived = false;
                for(int attemptNumber = 0; attemptNumber < 3; attemptNumber++)
                {
                    Console.WriteLine("Sending Heartbeat");
                    _servingNodeClient.SendHeartbeat(_clientSettings.UserId);
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
                if(_protocolSwitch.ServingNodeEndpoint == null)
                {
                    Console.WriteLine($"Requesting Serving Node from LoadBalancer {_loadBalancerEndPoint}");
                    var servingNodeEndpoint = await _loadBalancerProtocol.RequestServingNode(_loadBalancerEndPoint);
                    if(servingNodeEndpoint == null)
                    {
                        Console.WriteLine("Failed to get a serving node");
                        if(await WaitForCancel(token, 2000)) 
                        {
                            return;
                        }
                        continue;
                    }
                    _protocolSwitch.ServingNodeEndpoint = servingNodeEndpoint;
                }

                Console.WriteLine($"Got serving node at {_protocolSwitch.ServingNodeEndpoint}");

                _servingNodeClient.Register(_clientSettings.UserId);
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
                _servingNodeClient.Deregister(_registeredUserId);
                await WaitForCancel(token, 2000);
            }
        }

        public void StartCall(ushort groupId)
        {
            _callGroup = groupId;
            _stateManager.HandleEvent(EventId.CallRequest);
        }

        public ushort CallGroup => _callGroup;

        async Task StartCall(CancellationToken token)
        {
            if(_callGroup == 0)
            {
                _callGroup = IdleGroup;
            }
            var ignore = _mediaClient.StartSendingAudio(_callGroup);

            Console.WriteLine($"sending StartGroupCall for group {_callGroup} {_protocolSwitch.ServingNodeEndpoint}");
            while(!token.IsCancellationRequested)
            {
                _servingNodeClient.StartGroupCall(_clientSettings.UserId, _callGroup);
                await WaitForCancel(token, 1000);
            }
        }

        public ushort IdleGroup
        {
            get => _idleGroup;
            set => _idleGroup = value;
        }

        bool IsPttDown
        {
            get;
            set;
        }

        public void PttUp()
        {
            IsPttDown = false;
            _stateManager.HandleEvent(EventId.PttUp);
        }

        public void PttDown()
        {
            IsPttDown = true;
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

        public uint? Talker
        {
            get;
            set;
        }

        public void HandleFloorTaken(ushort groupId, uint userId)
        {
            _callGroup = groupId;
            Talker = userId;
            
            if(_clientSettings.UserId == userId)
            {
                _stateManager.HandleEvent(EventId.FloorGranted);
                return;
            }
            _stateManager.HandleEvent(EventId.FloorTaken);
        }

        public void HandleFloorIdle(ushort groupId)
        {
            _callGroup = groupId;
            Talker = null;
            _stateManager.HandleEvent(EventId.FloorIdle);
        }
    }
}