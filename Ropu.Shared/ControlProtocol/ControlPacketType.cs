namespace Ropu.Shared.ControlProtocol
{
    public enum ControlPacketType
    {
        Registration = 0,
        RegistrationResponse = 1,
        CallEnded = 5,
        StartGroupCall = 9,
        CallStarted = 10,
        CallStartFailed = 11
    }
}