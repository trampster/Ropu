using System;

namespace RopuForms.Droid.AAudio
{
    public class Resampler
    {
        int _index = 0;
        short[] _history = new short[256];

        /// <summary>
        /// Filter taps designed using http://t-filter.engineerjs.com/
        /// Pass 0 -> 3k
        /// Stop 5k -> 24k
        /// </summary>
        short[] _taps = new short[]
        {
            62, -176, -329, -556, -802, -1005, -1090, -985,
            636, -23, 826, 1837, 894, 3859, 4595, 4994,
            4994, 4595, 3859, 2894, 1837, 826, -23, -636,
            -985, -1090, -1005, -802, -556, -329, -176, 62
        };

        void LowFilter(Span<short> buffer)
        {
            int hmask = _taps.Length - 1;

            for (int index = 0; index < buffer.Length; index++)
            {
                long acc = 0;

                _history[_index & hmask] = buffer[index];

                _index++;

                for (int tapIndex = 0; tapIndex < _taps.Length; tapIndex++)
                {
                    int j = _index - tapIndex;
                    acc += (long)_history[j & hmask] * _taps[tapIndex];
                }

                if (acc > 0x3fffffff)
                    acc = 0x3fffffff;
                else if (acc < -0x40000000)
                    acc = -0x40000000;

                buffer[index] = (short)(acc >> 15);
            }
        }

        public void UpSample(Span<short> input, Span<short> output)
        {
            Expand(input, output);
            LowFilter(output);
        }

        public void DownSample(Span<short> input, Span<short> output)
        {
            LowFilter(input);
            Decimate(input, output, 6);
        }

        void Expand(Span<short> input, Span<short> output)
        {
            int ratio = output.Length / input.Length;
            short last = 0;
            for (int outputIndex = 0; outputIndex < output.Length; outputIndex++)
            {
                if ((outputIndex % ratio) == 0)
                {
                    last = input[outputIndex / ratio];
                    output[outputIndex] = last;
                }
                else
                {
                    output[outputIndex] = last;
                }
            }
        }

        void Decimate(Span<short> input, Span<short> output, int ratio)
        {
            for (int outputIndex = 0; outputIndex < output.Length; outputIndex++)
            {
                output[outputIndex] = input[outputIndex * ratio];
            }
        }
    }
}