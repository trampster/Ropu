namespace Ropu.Shared.ControlProtocol
{
    public enum RopuPacketType
    {
        //Control Protocol
        Registration = 0,
        RegistrationResponse = 1,
        CallEnded = 2,
        StartGroupCall = 3,
        CallStartFailed = 5,
        //Floor Control
        FloorDenied = 6,
        FloorTaken = 7,
        FloorIdle = 8,
        FloorReleased = 9,
        FloorRequest = 10,
        //Media 
        MediaPacketIndivdualCall = 11,
        MediaPacketGroupCall = 12,
        Heartbeat = 13,

        //more control
        HeartbeatResponse = 14,
        NotRegistered = 15,
        Deregister = 16,
        DeregisterResponse = 17
    }
}