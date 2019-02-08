using System.Net;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public interface IControllingFunctionPacketHandler
    {
        void HandleRegistrationResponseReceived(Codec codec, ushort bitrate);
        void HandleCallStartFailed(CallFailedReason reason);
        void HandleHeartbeatResponseReceived();
        void HandleNotRegisteredReceived();
        void HandleRegisterResponse();
        void HandleCallEnded(ushort groupId);
        void HandleFloorTaken(ushort groupId, uint userId);
        void HandleFloorIdle(ushort groupId);
    }
}