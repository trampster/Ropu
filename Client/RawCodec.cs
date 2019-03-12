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
                BitConverter.TryWriteBytes(output.Slice(rawIndex*2), raw[rawIndex]);
            }
            return raw.Length*2;
        }


        public int Decode(AudioData encodedData, bool isNext, short[] output)
        {
            if(encodedData == null)
            {
                //return silence
                for(int index = 0; index < output.Length; index++)
                {
                    output[index] = 0;
                }
                return 160;
            }
            var encoded = encodedData.Data;
            if(encoded.Length == 0)
            {
                //silence
                for(int index = 0; index < output.Length; index++)
                {
                    output[index] = 0;
                }
                return output.Length;
            }
            int outputIndex = 0;
            for(int encodedIndex = 0; encodedIndex < encoded.Length; encodedIndex += 2)
            {
                var value = BitConverter.ToInt16(encoded.Slice(encodedIndex));
                output[outputIndex] = value;
                outputIndex++;
            }
            return encoded.Length/2;
        }
    }
}