using Ropu.Client.StateModel;

namespace Ropu.Client
{
    public enum StateId
    {
        Start,
        Unregistered,
        Registered,
        StartingCall,
        CallInProgress,
        Deregistering
    }

    public enum EventId
    {
        RegistrationResponseReceived,
        CallRequest,
        CallStarted,
        CallStartFailed,
        HeartbeatFailed,
        NotRegistered,
        UserIdChanged,
        DeregistrationResponseReceived,
        PttDown,
        PttUp,
        CallEnded
    }

    public class RopuState : State<StateId, EventId>
    {
        public RopuState(StateId identifier) : base(identifier)
        {
        }
    }
}