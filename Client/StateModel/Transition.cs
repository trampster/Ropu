using System;

namespace Ropu.Client.StateModel
{
    public class Transition<EventT, StateT> where EventT : struct
    {
        readonly Func<StateT> _getState;

        public Transition(EventT eventType, Func<StateT> getState)
        {
            Event = eventType;
            _getState = getState;
        }

        public EventT Event {get;}
        public StateT State => _getState();
    }
}