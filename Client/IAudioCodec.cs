using System;
using Ropu.Client.JitterBuffer;

namespace Ropu.Client
{
    public interface IAudioCodec
    {
        /// <summary>
        /// Encodes the audio
        /// </summary>
        /// <param name="raw">160 frames to encode</param>
        /// <param name="output">span to put encoded output into</param>
        /// <returns>the number of bytes writen to output</returns>
        int Encode(short[] raw, Span<byte> output);

        int Decode(AudioData? audioData, bool isNext, short[] output);
    }
}