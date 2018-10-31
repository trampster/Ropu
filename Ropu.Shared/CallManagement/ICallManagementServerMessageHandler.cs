using System.Net;

namespace Ropu.Shared.CallManagement
{
    public interface ICallManagementServerMessageHandler
    {
        void HandleRegisterMediaController(IPAddress from, ushort requestId, ushort controlPort, IPEndPoint mediaEndpoint);
        void HandleRegisterFloorController(IPAddress from, ushort requestId, ushort controlPort, IPEndPoint floorControlEndpoint);
        void HandleRequestServingNode(ushort requestId, IPEndPoint endPoint);
    }
}