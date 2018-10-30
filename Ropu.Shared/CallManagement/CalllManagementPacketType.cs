namespace Ropu.Shared.CallManagement
{
    public enum CallManagementPacketType
    {
        RegisterMediaController = 0,
        RegisterFloorController = 1,
        StartCall = 2,
        Ack = 3,
        RegistrationUpdate = 11,
        RegistrationRemoved = 12
    }
}