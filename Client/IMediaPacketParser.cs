using System;

namespace Ropu.Client
{
    public interface IMediaPacketParser
    {
        void ParseMediaPacketGroupCall(Span<byte> data);
    }
}