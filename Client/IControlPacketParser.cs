using System;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public interface IControlPacketParser
    {
        void ParseRegistrationResponse(Span<byte> data);
        void ParseCallEnded(Span<byte> data);
        void ParseCallStartFailed(Span<byte> data);
        void ParseHeartbeatResponse(Span<byte> data);
        void ParseNotRegistered(Span<byte> data);
        void ParseDeregisterResponse(Span<byte> data);
        void ParseFloorTaken(Span<byte> data);
        void ParseFloorIdle(Span<byte> data);
    }
}