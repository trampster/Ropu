using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ropu.Shared;

namespace Ropu.Client
{
    public class RopuClient : IControllingFunctionPacketHandler
    {
        const uint _userId = 1234;
        const ushort _rtpPort = 1000;
        const ushort _floorControlPort = 1002;

        ControllingFunctionClient _controllingFunctionClient;
        int _state = (int)State.Unregistered;

        State CurrentState => (State)_state;

        enum State
        {
            Unregistered,
            Registered, //registered but not in a call or trying to make a call
        }

        readonly System.Timers.Timer _regisrationAttemptTimer;

        public RopuClient(ControllingFunctionClient controllingFunctionClient)
        {
            _controllingFunctionClient = controllingFunctionClient;
            _controllingFunctionClient.SetControllingFunctionHandler(this);
            _regisrationAttemptTimer = new System.Timers.Timer();
            _regisrationAttemptTimer.Interval = 5000;
            _regisrationAttemptTimer.AutoReset = false;
            _regisrationAttemptTimer.Elapsed += (sender, args) => RegistrationAttemptTimerExpired();
        }

        public void Start()
        {
            _controllingFunctionClient.StartListening();
            Register();
        }

        void RegistrationAttemptTimerExpired()
        {
            if(CurrentState == State.Unregistered)
            {
                Register();
            }
        }

        void Register()
        {
            Console.WriteLine("Sending Registration");
            _controllingFunctionClient.Register(_userId, _rtpPort, _floorControlPort);
            _regisrationAttemptTimer.Start();
        }

        public void RegistrationResponseReceived(Codec codec, ushort bitrate)
        {
            SetState(State.Registered, State.Unregistered); //set the state to registered only if it currently is unregistered
            _regisrationAttemptTimer.Stop();
        }

        bool SetState(State newState, State currentState)
        {
            State was = (State)Interlocked.CompareExchange(ref _state, (int)newState, (int)currentState);
            if(was != currentState)
            {
                return false;//someone else changed the state, caller should reevaluate if this state transition is still valid.
            }
            Console.WriteLine($"State changed {currentState} -> {newState}");
            return true;
        }
    }
}