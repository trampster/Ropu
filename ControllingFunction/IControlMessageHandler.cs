using System.Net;

namespace Ropu.ControllingFunction
{
    public interface IControlMessageHandler
    {
        void Registration(uint userId, IPEndPoint endPoint);
        void StartGroupCall(uint userId, ushort groupId);
    }
}