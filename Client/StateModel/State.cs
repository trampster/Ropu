using System;
using System.Collections.Generic;

namespace Ropu.Client.StateModel
{
    public class State<Id, EventT> : IState<Id, EventT>
    {
        readonly List<Transition<EventT, IState<Id, EventT>>> _transitions;

        public State(Id identifier)
        {
            _transitions = new List<Transition<EventT, IState<Id, EventT>>>();
            Identifier = identifier;
            Entry = () => {};
            Exit = () => {};
        }

        public Id Identifier {get;}
        public  Action Entry {get;set;}
        public Action Exit {get;set;}

        public void AddTransition(EventT eventId, Func<IState<Id, EventT>> getState)
        {
            _transitions.Add(new Transition<EventT, IState<Id, EventT> >(eventId, getState));
        }

        public IState<Id, EventT> Transition(EventT eventType)
        {
            foreach(var transition in _transitions)
            {
                if(transition.Event.Equals(eventType))
                {
                    return transition.State;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return Identifier.ToString();
        }
    }
}