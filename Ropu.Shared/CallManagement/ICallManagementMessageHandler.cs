using System.Net;

namespace Ropu.Shared.CallManagement
{
    public interface ICallManagementServerMessageHandler
    {
        void HandleRegisterMediaController(IPAddress from, uint requestId, ushort controlPort, IPEndPoint mediaEndpoint);
        void HandleRegisterFloorController(IPAddress from, uint requestId, ushort controlPort, IPEndPoint floorControlEndpoint);
    }
}