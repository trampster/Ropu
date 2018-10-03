using Ropu.Client.StateModel;

namespace Ropu.Client
{
    public enum StateId
    {
        Start,
        Unregistered,
        Registered
    }

    public enum EventId
    {
        RegistrationResponseReceived
    }

    public class RopuState : State<StateId, EventId>
    {
        public RopuState(StateId identifier) : base(identifier)
        {
        }
    }
}