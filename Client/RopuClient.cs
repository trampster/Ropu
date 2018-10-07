using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ropu.Client.StateModel;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class RopuClient : IControllingFunctionPacketHandler
    {
        const uint _userId = 1234;
        const ushort _rtpPort = 1000;
        const ushort _floorControlPort = 1002;

        ControllingFunctionClient _controllingFunctionClient;

        RopuState _start;
        RopuState _registered;
        RopuState _unregistered;
        RopuState _startingCall;
        StateManager<EventId> _stateManager;

        readonly Ropu.Shared.Timer _retryTimer;

        public RopuClient(ControllingFunctionClient controllingFunctionClient)
        {
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

            _stateManager = new StateManager<EventId>(_start);
        }

        System.Timers.Timer CreateTimer(int interval, Action callback)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = interval;
            timer.AutoReset = false;
            timer.Elapsed += (sender, args) => callback();
            return timer;
        }

        public void Start()
        {
            _controllingFunctionClient.StartListening();
            _stateManager.SetState(_unregistered, _start);
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
            Console.WriteLine("Sending Registration");
            _controllingFunctionClient.Register(_userId, _rtpPort, _floorControlPort);
            _retryTimer.Duration = 2000;
            _retryTimer.Callback = Register;
            _retryTimer.Start();
        }

        public void StartCall(ushort groupId)
        {
            Console.WriteLine("sending StartGroupCall");
            _controllingFunctionClient.StartGroupCall(_userId, groupId);
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

        public void HandleCallStarted(uint groupId, ushort callId, IPEndPoint mediaEndpoint, IPEndPoint floorControlEndpoint)
        {
            _stateManager.HandleEvent(EventId.CallRequest);
        }

        public void HandleCallStartFailed(CallFailedReason reason)
        {
            Console.WriteLine($"CallStartFailed with reason {reason}");
            _stateManager.HandleEvent(EventId.CallStartFailed);

        }
    }
}