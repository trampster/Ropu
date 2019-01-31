using System;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public interface IControlPacketParser
    {
        void ParseRegistrationResponse(Span<byte> data);
        void ParseCallEnded(Span<byte> data);
        void ParseCallStarted(Span<byte> data);
        void ParseCallStartFailed(Span<byte> data);
        void ParseHeartbeatResponse(Span<byte> data);
        void ParseNotRegistered(Span<byte> data);
        void ParseDeregisterResponse(Span<byte> data);
    }
}