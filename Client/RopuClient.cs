using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ropu.Client.StateModel;
using Ropu.Shared;

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
        StateManager<EventId> _stateManager;

        readonly System.Timers.Timer _regisrationAttemptTimer;

        public RopuClient(ControllingFunctionClient controllingFunctionClient)
        {
            _controllingFunctionClient = controllingFunctionClient;
            _controllingFunctionClient.SetControllingFunctionHandler(this);
            _regisrationAttemptTimer = new System.Timers.Timer();
            _regisrationAttemptTimer.Interval = 5000;
            _regisrationAttemptTimer.AutoReset = false;
            _regisrationAttemptTimer.Elapsed += (sender, args) => RegistrationAttemptTimerExpired();
            
            _start = new RopuState(StateId.Start);
            _registered = new RopuState(StateId.Registered);
            _unregistered = new RopuState(StateId.Unregistered)
            {
                Entry = () => Register(),
                Exit = () => _regisrationAttemptTimer.Stop(),
            };
            _unregistered.AddTransition(EventId.RegistrationResponseReceived, _registered);

            _stateManager = new StateManager<EventId>(_start);
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

        void Register()
        {
            Console.WriteLine("Sending Registration");
            _controllingFunctionClient.Register(_userId, _rtpPort, _floorControlPort);
            _regisrationAttemptTimer.Start();
        }

        public void RegistrationResponseReceived(Codec codec, ushort bitrate)
        {
            _stateManager.HandleEvent(EventId.RegistrationResponseReceived);
        }
    }
}