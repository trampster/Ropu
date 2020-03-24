using System;
using Ropu.Client;
using Ropu.Client.JitterBuffer;

namespace Client.NoAudio
{
    public class NoAudioCodec : IAudioCodec
    {
        public int Decode(AudioData? audioData, bool isNext, short[] output)
        {
            return 160;
        }

        public int Encode(short[] raw, Span<byte> output)
        {
            return 160;
        }
    }
}
