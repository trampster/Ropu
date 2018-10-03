namespace Ropu.Client.StateModel
{
    public class Transition<EventT, StateT>
    {
        public Transition(EventT eventType, StateT state)
        {
            Event = eventType;
            State = state;
        }

        public EventT Event {get;}
        public StateT State {get;}
    }
}