using Ropu.Client.StateModel;

namespace Ropu.Client
{
    public enum StateId
    {
        Start,
        Unregistered,
        Registered,
        StartingCall,
        CallInProgress
    }

    public enum EventId
    {
        RegistrationResponseReceived,
        CallRequest,
        CallStarted,
    }

    public class RopuState : State<StateId, EventId>
    {
        public RopuState(StateId identifier) : base(identifier)
        {
        }
    }
}