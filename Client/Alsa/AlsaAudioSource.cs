using System;
using System.Diagnostics;

namespace Ropu.Client.Alsa
{

    public class AlsaAudioSource : IAudioSource
    {
        public void ReadAudio(byte[] buffer)
        {
        }

        public void Start()
        {
            IntPtr pcmPtr;
            int error = AlsaNativeMethods.snd_pcm_open(out pcmPtr, "default", snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE, 0);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_open));
            }

            IntPtr hardwareParamsPtr;
            error = AlsaNativeMethods.snd_pcm_hw_params_malloc(out hardwareParamsPtr);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_malloc));
            }

            error = AlsaNativeMethods.snd_pcm_hw_params_any(pcmPtr, hardwareParamsPtr);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_any));
            }
            error = AlsaNativeMethods.snd_pcm_hw_params_set_access(pcmPtr, hardwareParamsPtr, snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_access));
            }

            error = AlsaNativeMethods.snd_pcm_hw_params_set_format(pcmPtr, hardwareParamsPtr, snd_pcm_format_t.SND_PCM_FORMAT_S16_LE);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_format));
            }

            uint rate = 8000;
            int dir = 0;
            error = AlsaNativeMethods.snd_pcm_hw_params_set_rate_near(pcmPtr, hardwareParamsPtr, ref rate, ref dir);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_rate_near));
            }
            if(rate != 8000)
            {
                throw new Exception($"Could not get required sample rate of {8000} instead got {rate}");
            }

            error = AlsaNativeMethods.snd_pcm_hw_params_set_channels(pcmPtr, hardwareParamsPtr, 1);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_channels));
            }

            error = AlsaNativeMethods.snd_pcm_hw_params_set_period_size(pcmPtr, hardwareParamsPtr, 160, 0);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_period_size));
            }

            // error = AlsaNativeMethods.snd_pcm_hw_params_set_periods(pcmPtr, hardwareParamsPtr, 2, 0);
            // if(error < 0)
            // {
            //     throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params_set_periods));
            // }

            error = AlsaNativeMethods.snd_pcm_hw_params(pcmPtr, hardwareParamsPtr);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_hw_params));
            }

            AlsaNativeMethods.snd_pcm_hw_params_free(hardwareParamsPtr);

            error = AlsaNativeMethods.snd_pcm_prepare(pcmPtr);
            if(error < 0)
            {
                throw new AlsaNativeError(error, nameof(AlsaNativeMethods.snd_pcm_prepare));
            }
            short[] buffer = new short[160];
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            long lastMilliseconds = stopwatch.ElapsedMilliseconds;

            while(true)
            {
                
                int ammountRead = AlsaNativeMethods.snd_pcm_readi(pcmPtr, buffer, (uint)buffer.Length);
                var time = stopwatch.ElapsedMilliseconds;
                if(ammountRead == 160)
                {
                    var ellapsed = time - lastMilliseconds;
                    lastMilliseconds = time;
                    Console.WriteLine($"Success Read 160 frames after {ellapsed} ms");
                }
                else
                {
                    Console.WriteLine("$Failure Read {ammountRead} frames");
                }
            }
        }

        public void Stop()
        {
        }
    }

}