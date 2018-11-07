using System.Net;

namespace Ropu.LoadBalancer
{
    public interface IControlMessageHandler
    {
        void Registration(uint userId, IPEndPoint endPoint);
        void StartGroupCall(uint userId, ushort groupId, IPEndPoint endPoint);
    }
}