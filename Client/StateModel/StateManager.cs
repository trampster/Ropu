using System;
using System.Threading;

namespace Ropu.Client.StateModel
{
    public class StateManager<EventT>
    {
        IState<EventT> _current;

        public StateManager(IState<EventT> start)
        {
            _current = start;
        }

        public IState<EventT> CurrentState
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
        public IState<EventT> SetState(IState<EventT> newState, IState<EventT> expected)
        {
            IState<EventT> original = Interlocked.CompareExchange(ref _current, newState, expected);
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
                    Console.WriteLine($"State Transition {current} -> {newState}");
                    break;
                }
                //state changed while we where trying to transition, try the event against the new state.
            }
        }

    }
}