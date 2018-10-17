namespace Ropu.Shared.CallManagement
{
    public enum CallManagementPacketType
    {
        RegisterMediaController = 0,
        RegisterFloorController = 1,
        StartCall = 2,
        Ack = 3,
        GetGroupsFileRequest = 4,
        GroupGroupFileRequest = 5,
        FileManifestResponse = 6,
        FilePartRequest = 7,
        FilePartResponse = 8,
        FilePartUnrecognized = 9,
        CompleteFileTransfer = 10,
        RegistrationUpdate = 11,
        RegistrationRemoved = 12
    }
}