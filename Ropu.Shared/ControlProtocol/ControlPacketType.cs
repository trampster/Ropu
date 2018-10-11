namespace Ropu.Shared.ControlProtocol
{
    public enum CombinedPacketType
    {
        //Control Protocol
        Registration = 0,
        RegistrationResponse = 1,
        CallEnded = 2,
        StartGroupCall = 3,
        CallStarted = 4,
        CallStartFailed = 5,
        //Floor Control
        FloorDenied = 6,
        FloorGranted = 7,
        FloorReleased = 8,
        //Media 
        Media = 9
    }
}