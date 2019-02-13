using System;

namespace Ropu.Client.Alsa
{
    public class SoundPcmHardwareParams : IDisposable
    {
        IntPtr _hardwareParamsPtr;
        readonly IntPtr _pcmPtr;

        public SoundPcmHardwareParams(SoundPcm pcm)
        {
            _pcmPtr = pcm.Ptr;
            int error = AlsaNativeMethods.snd_pcm_hw_params_malloc(out _hardwareParamsPtr);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_malloc));
            }
        }
        
        internal IntPtr Ptr => _hardwareParamsPtr;

        /// <summary>
        /// would be nice to know what this does...
        /// </summary>
        public void Any()
        {
            int error = AlsaNativeMethods.snd_pcm_hw_params_any(_pcmPtr, _hardwareParamsPtr);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_any));
            }
        }

        public snd_pcm_access_t Access
        {
            set
            {
                int error = AlsaNativeMethods.snd_pcm_hw_params_set_access(_pcmPtr, _hardwareParamsPtr, value);
                if(error < 0)
                {
                    throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_access));
                }
            }
        }

        public snd_pcm_format_t Format
        {
            set
            {
                int error = AlsaNativeMethods.snd_pcm_hw_params_set_format(_pcmPtr, _hardwareParamsPtr, value);
                if(error < 0)
                {
                    throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_access));
                }
            }
        }

        public void SetRateNear(ref uint rate, ref int dir)
        {
            int error = AlsaNativeMethods.snd_pcm_hw_params_set_rate_near(_pcmPtr, _hardwareParamsPtr, ref rate, ref dir);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_rate_near));
            }
        }

        public uint Channels
        {
            set
            {
                int error = AlsaNativeMethods.snd_pcm_hw_params_set_channels(_pcmPtr, _hardwareParamsPtr, value);
                if(error < 0)
                {
                    throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_channels));
                }
            }
        }

        public void SetPeriodSize(uint periodSize, int dir)
        {
            int error = AlsaNativeMethods.snd_pcm_hw_params_set_period_size(_pcmPtr, _hardwareParamsPtr, periodSize, dir);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_period_size));
            }
        }

        public void SetPeriods(uint periods, int dir)
        {
            int error = AlsaNativeMethods.snd_pcm_hw_params_set_periods(_pcmPtr, _hardwareParamsPtr, periods, dir);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_periods));
            }
        }

        bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    AlsaNativeMethods.snd_pcm_hw_params_free(_hardwareParamsPtr);
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