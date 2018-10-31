namespace Ropu.Shared.CallManagement
{
    public enum CallManagementPacketType
    {
        RegisterMediaController = 0,
        RegisterFloorController = 1,
        StartCall = 2,
        Ack = 3,
        RegistrationUpdate = 4,
        RegistrationRemoved = 5,
        RequestServingNode = 6,
        ServingNodeResponse = 7
    }
}