using Ropu.Client.StateModel;

namespace Ropu.Client
{
    public enum StateId
    {
        Start,
        Unregistered,
        Registered,
        NoGroup,
        StartingCall,
        InCallIdle,
        InCallTransmitting,
        InCallReceiving,
        Deregistering,
        InCallReleasingFloor,
        InCallRequestingFloor
    }

    public enum EventId
    {
        RegistrationResponseReceived,
        CallRequest,
        CallStartFailed,
        HeartbeatFailed,
        NotRegistered,
        DeregistrationResponseReceived,
        PttDown,
        PttUp,
        CallEnded,
        FloorIdle,
        FloorTaken, //someone else got the floor
        FloorGranted, //we got the floor
        GroupSelected
    }

    public class RopuState : State<StateId, EventId>
    {
        public RopuState(StateId identifier) : base(identifier)
        {
        }
    }
}