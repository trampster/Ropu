using System;

namespace Ropu.Client.Alsa
{
    public class SoundPcm : IDisposable
    {
        IntPtr _pcmPtr;
        public SoundPcm(string name, snd_pcm_stream_t streamType, int mode)
        {
            int error = AlsaNativeMethods.snd_pcm_open(out _pcmPtr, name, streamType, mode);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_open));
            }
        }

        internal IntPtr Ptr => _pcmPtr;

        public SoundPcmHardwareParams HardwareParams
        {
            set
            {
                int error = AlsaNativeMethods.snd_pcm_hw_params(_pcmPtr, value.Ptr);
                if(error < 0)
                {
                    throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params));
                }
            }
        }

        public void Prepare()
        {
            int error = AlsaNativeMethods.snd_pcm_prepare(_pcmPtr);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_prepare));
            }
        }

        public int ReadInterleaved(short[] buffer, uint length)
        {
            return AlsaNativeMethods.snd_pcm_readi(_pcmPtr, buffer, length);
        }

        bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    AlsaNativeMethods.snd_pcm_close(_pcmPtr);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }
    }
}