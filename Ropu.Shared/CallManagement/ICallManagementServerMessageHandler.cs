using System.Net;

namespace Ropu.Shared.CallManagement
{
    public interface ICallManagementServerMessageHandler
    {
        void HandleRegisterMediaController(IPAddress from, ushort requestId, ushort controlPort, IPEndPoint mediaEndpoint);
        void HandleRegisterFloorController(IPAddress from, ushort requestId, ushort controlPort, IPEndPoint floorControlEndpoint);
        void HandleGetGroupsFileRequest(IPEndPoint from, ushort requestId);
        void HandleGetGroupFileRequest(IPEndPoint from, ushort requestId, ushort groupId);
        void HandleFilePartRequest(IPEndPoint from, ushort requestId, ushort fileId, ushort partNumber);
        void HandleCompleteFileTransfer(IPEndPoint from, ushort requestId, ushort fileId);
    }
}