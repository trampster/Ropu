using System;
using System.Threading.Tasks;

namespace Ropu.Client
{
    public interface IAudioSource : IDisposable
    {
        /// <summary>
        /// Read 20 ms of audio at 8000 samples/s (160 samples)
        /// Should block until buffer is filled or source is stopped
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        void ReadAudio(short[] buffer);

        void Start();

        void Stop();
    }
}