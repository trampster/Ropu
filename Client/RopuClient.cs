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

        readonly ControllingFunctionClient _controllingFunctionClient;
        readonly ProtocolSwitch _protocolSwitch;
        readonly IClientSettings _clientSettings;

        IPEndPoint _servingNodeEndpoint;

        RopuState _start;
        RopuState _registered;
        RopuState _unregistered;
        RopuState _startingCall;
        RopuState _callInProgress;
        StateManager<EventId> _stateManager;
        LoadBalancerProtocol _loadBalancerProtocol;

        readonly Ropu.Shared.Timer _retryTimer;
        readonly IPAddress _ipAddress;
        readonly IPEndPoint _loadBalancerEndPoint;

        public RopuClient(
            ProtocolSwitch protocolSwitch, 
            ControllingFunctionClient controllingFunctionClient, 
            IPAddress address,
            LoadBalancerProtocol loadBalancerProtocol,
            IPEndPoint loadBalancerEndPoint,
            IClientSettings clientSettings)
        {
            _clientSettings = clientSettings;
            _loadBalancerEndPoint = loadBalancerEndPoint;
            _loadBalancerProtocol = loadBalancerProtocol;
            _protocolSwitch = protocolSwitch;
            _controllingFunctionClient = controllingFunctionClient;
            _controllingFunctionClient.SetControllingFunctionHandler(this);
            _retryTimer = new Ropu.Shared.Timer();
            _start = new RopuState(StateId.Start);
            _registered = new RopuState(StateId.Registered);
            _registered.AddTransition(EventId.CallRequest, () => _startingCall);
            _unregistered = new RopuState(StateId.Unregistered)
            {
                Entry = () => Register(),
                Exit = () => _retryTimer.Cancel(),
            };
            _unregistered.AddTransition(EventId.RegistrationResponseReceived, () => _registered);
            _startingCall = new RopuState(StateId.StartingCall)
            {
                Exit = () => _retryTimer.Cancel()
            };
            _startingCall.AddTransition(EventId.CallStartFailed, () => _registered);
            _startingCall.AddTransition(EventId.CallStarted, () => _callInProgress);
            _callInProgress = new RopuState(StateId.CallInProgress);

            _stateManager = new StateManager<EventId>(_start);
            _ipAddress = address;
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

        void RegistrationAttemptTimerExpired()
        {
            Register();
        }

        void StartCallTimerExpired()
        {
            Register();
        }

        void Register()
        {
            while(_servingNodeEndpoint == null)
            {
                Console.WriteLine($"Requesting Serving Node from LoadBalancer {_loadBalancerEndPoint}");
                _servingNodeEndpoint = _loadBalancerProtocol.RequestServingNode(_loadBalancerEndPoint).Result;
                if(_servingNodeEndpoint == null)
                {
                    Console.WriteLine("Failed to get a serving node");
                    Task.Delay(500).Wait();
                }
            }

            Console.WriteLine($"Got serving node at {_servingNodeEndpoint}");

            _controllingFunctionClient.Register(_clientSettings.UserId, _servingNodeEndpoint);
            _retryTimer.Duration = 2000;
            _retryTimer.Callback = Register;
            _retryTimer.Start();
        }

        public void StartCall(ushort groupId)
        {
            Console.WriteLine($"sending StartGroupCall to {_servingNodeEndpoint}");
            _controllingFunctionClient.StartGroupCall(_clientSettings.UserId, groupId, _servingNodeEndpoint);
            StartRetryTimer(1000, () => StartCall(groupId));
            if(_stateManager.CurrentState != _startingCall)
            {
                _stateManager.HandleEvent(EventId.CallRequest);
            }
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
    }
}