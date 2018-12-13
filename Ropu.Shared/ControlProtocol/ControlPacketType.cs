namespace Ropu.Shared.ControlProtocol
{
    public enum RopuPacketType
    {
        //Control Protocol
        Registration = 0,
        RegistrationResponse = 1,
        CallEnded = 2,
        StartGroupCall = 3,
        GroupCallStarted = 4,
        CallStartFailed = 5,
        //Floor Control
        FloorDenied = 6,
        FloorGranted = 7,
        FloorReleased = 8,
        //Media 
        MediaPacketIndivdualCall = 9,
        MediaPacketGroupCall = 10
    }
}