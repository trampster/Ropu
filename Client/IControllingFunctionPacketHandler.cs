using System.Net;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public interface IControllingFunctionPacketHandler
    {
        void HandleRegistrationResponseReceived(Codec codec, ushort bitrate);
        void HandleCallStarted(ushort groupId, uint userId);
        void HandleCallStartFailed(CallFailedReason reason);
        void HandleHeartbeatResponseReceived();
    }
}