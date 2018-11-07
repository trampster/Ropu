using System.Net;

namespace Ropu.ServingNode
{
    public interface IMessageHandler
    {
        void Registration(uint userId, IPEndPoint endPoint);
        void StartGroupCall(uint userId, ushort groupId, IPEndPoint endPoint);
    }
}