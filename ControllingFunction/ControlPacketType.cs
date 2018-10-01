namespace Ropu.ContollingFunction
{
    public enum ControlPacketType
    {
        Registration = 0,
        RegistrationResponse = 1,
        FloorDenied = 2,
        FloorGranted = 3,
        FloorReleased = 4,
        CallEnded = 5,
        GroupSubscribe = 6,
        GroupUnsubscribe = 7,
        Presence = 8,
        StartGroupCall = 9,
        StartCallResponse = 10
    }
}