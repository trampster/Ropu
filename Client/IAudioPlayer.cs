using System;

namespace Ropu.Client
{
    public interface IAudioPlayer : IDisposable
    {
        /// <summary>
        /// Play 20 ms of audio at 8000 samples/s (160 samples)
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        void PlayAudio(short[] buffer);
    }
}