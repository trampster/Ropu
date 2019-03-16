using System;

namespace Ropu.Client.JitterBuffer
{
    public class AudioData
    {
        byte[] _data = new byte[320];

        public Span<byte> Data
        {
            get => _data.AsSpan(0, Length);
            set
            {
                for(int index = 0; index < value.Length; index++)
                {
                    _data[index] = value[index];
                }
                Length = value.Length;
            }
        }

        public byte[] Buffer => _data;

        public int Length
        {
            get;
            private set;
        }
    }
}