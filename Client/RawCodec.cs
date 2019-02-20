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

        public int Decode(AudioData encodedData, short[] output)
        {
            var encoded = encodedData.Data;
            int outputIndex = 0;
            for(int encodedIndex = 0; encodedIndex < encoded.Length; encodedIndex += 2)
            {
                var value = 
                    (encoded[encodedIndex] << 8) + 
                    encoded[encodedIndex+1];
                output[outputIndex] = (short)value;
                outputIndex++;
            }
            return encoded.Length/2;
        }
    }
}