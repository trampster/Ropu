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
        MediaPacketGroupCallClient = 12,
        MediaPacketGroupCallServingNode = 13,
        Heartbeat = 14,

        //more control
        HeartbeatResponse = 15,
        NotRegistered = 16,
        Deregister = 17,
        DeregisterResponse = 18
    }
}