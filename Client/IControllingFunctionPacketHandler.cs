using System.Net;
using Ropu.Shared;

namespace Ropu.Client
{
    public interface IControllingFunctionPacketHandler
    {
        void RegistrationResponseReceived(Codec codec, ushort bitrate);
        void CallStarted(uint groupId, ushort callId, IPEndPoint mediaEndpoint, IPEndPoint floorControlEndpoint);
    }
}