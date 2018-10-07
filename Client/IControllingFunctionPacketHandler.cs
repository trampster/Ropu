using System.Net;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public interface IControllingFunctionPacketHandler
    {
        void HandleRegistrationResponseReceived(Codec codec, ushort bitrate);
        void HandleCallStarted(uint groupId, ushort callId, IPEndPoint mediaEndpoint, IPEndPoint floorControlEndpoint);
        void HandleCallStartFailed(CallFailedReason reason);
    }
}