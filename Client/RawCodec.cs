using System;
using Ropu.Shared;

namespace Ropu.Client
{
    public class RawCodec : IAudioCodec
    {
        public int Encode(short[] raw, Span<byte> output)
        {
            for(int rawIndex = 0; rawIndex < raw.Length; rawIndex++)
            {
                output.WriteShort(raw[rawIndex], rawIndex*2);
            }
            return raw.Length*2;
        }
    }
}