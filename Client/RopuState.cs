using Ropu.Client.StateModel;

namespace Ropu.Client
{
    public enum StateId
    {
        Start,
        Unregistered,
        Registered,
        StartingCall,
        InCallIdle,
        InCallTransmitting,
        InCallReceiving,
        Deregistering,
        InCallReleasingFloor
    }

    public enum EventId
    {
        RegistrationResponseReceived,
        CallRequest,
        CallStartFailed,
        HeartbeatFailed,
        NotRegistered,
        UserIdChanged,
        DeregistrationResponseReceived,
        PttDown,
        PttUp,
        CallEnded,
        FloorIdle,
        FloorTaken, //someone else got the floor
        FloorGranted //we got the floor
    }

    public class RopuState : State<StateId, EventId>
    {
        public RopuState(StateId identifier) : base(identifier)
        {
        }
    }
}