using System;
using System.Threading;

namespace Ropu.Client.StateModel
{
    public class StateManager<Id, EventT>
    {
        IState<Id, EventT> _current;
        public event EventHandler<EventArgs> StateChanged;

        public StateManager(IState<Id, EventT> start)
        {
            _current = start;
        }

        public IState<Id, EventT> CurrentState
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Changes the current state to new  if the current state is expected
        /// </summary>
        /// <param name="newState">The state to change to</param>
        /// <param name="expected">The expected current state, if this doesn't match then the state wont change</param>
        /// <returns>The original state</returns>
        public IState<Id, EventT> SetState(IState<Id, EventT> newState, IState<Id, EventT> expected)
        {
            IState<Id, EventT> original = Interlocked.CompareExchange(ref _current, newState, expected);
            if(original == expected)
            {
                expected.Exit();
                newState.Entry();
            }
            return original;
        }

        public void HandleEvent(EventT eventType)
        {
            Console.WriteLine($"Event Occured {eventType}");
            while(true) 
            {
                var current = CurrentState;
                var newState = current.Transition(eventType);
                if(newState == null) 
                {
                    Console.WriteLine($"No Transition from state {current} for event {eventType}");
                    return; //no transition defined
                }
                var original = SetState(newState, current);
                if(original == current)
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                    Console.WriteLine($"State Transition {current} -> {newState}");
                    break;
                }
                //state changed while we where trying to transition, try the event against the new state.
            }
        }

    }
}