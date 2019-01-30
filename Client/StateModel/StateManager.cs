using System;
using System.Collections.Generic;
using System.Threading;

namespace Ropu.Client.StateModel
{
    public class StateManager<Id, EventT>
    {
        IState<Id, EventT> _current;
        public event EventHandler<EventArgs> StateChanged;
        readonly List<IState<Id, EventT>> _states = new List<IState<Id, EventT>>();
        

        public StateManager(IState<Id, EventT> start)
        {
            _current = start;
        }

        public void AddState(IState<Id, EventT> state)
        {
            _states.Add(state);
        }

        public void AddTransitionToAll(EventT eventId, Func<State<Id, EventT>> getState, Func<Id, bool> filter)
        {
            foreach(var state in _states)
            {
                if(filter(state.Identifier))
                {
                    state.AddTransition(eventId, getState);
                }
            }
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
                StateChanged?.Invoke(this, EventArgs.Empty);
                Console.WriteLine($"State Transition {original} -> {newState}");
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
                    break;
                }

                //state changed while we where trying to transition, try the event against the new state.
            }
        }

    }
}