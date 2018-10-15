using System.Net;

namespace Ropu.Shared.CallManagement
{
    public interface ICallManagementServerMessageHandler
    {
        void HandleRegisterMediaController(IPAddress from, uint requestId, ushort controlPort, IPEndPoint mediaEndpoint);
        void HandleRegisterFloorController(IPAddress from, uint requestId, ushort controlPort, IPEndPoint floorControlEndpoint);
        void HandleGetGroupsFileRequest(IPEndPoint from, uint requestId);
        void HandleGetGroupFileRequest(IPEndPoint from, uint requestId, ushort groupId);
        void HandleFilePartRequest(IPEndPoint from, uint requestId, ushort fileId, ushort partNumber);
    }
}