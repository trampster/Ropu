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
        //Media 
        MediaPacketIndivdualCall = 9,
        MediaPacketGroupCall = 10,
        Heartbeat = 11,

        //more control
        HeartbeatResponse = 12,
        NotRegistered = 13,
        Deregister = 14,
        DeregisterResponse = 15
    }
}