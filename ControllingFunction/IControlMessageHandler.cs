using System.Net;

namespace Ropu.ControllingFunction
{
    public interface IControlMessageHandler
    {
        void Registration(uint userId, ushort rtpPort, ushort floorControlPort, IPEndPoint controlEndpoint);
        void StartGroupCall(uint userId, ushort groupId);
    }
}