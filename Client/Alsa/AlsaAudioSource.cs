using System;
using System.Diagnostics;

namespace Ropu.Client.Alsa
{

    public class AlsaAudioSource : IAudioSource
    {
        readonly SoundPcm _soundPcm;

        public AlsaAudioSource()
        {
            _soundPcm = new SoundPcm("default", snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE, 0);
            using(var hardwareParams = new SoundPcmHardwareParams(_soundPcm))
            {
                hardwareParams.Any();
                hardwareParams.Access = snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED;
                hardwareParams.Format = snd_pcm_format_t.SND_PCM_FORMAT_S16_LE;

                uint rate = 8000;
                int dir = 0;
                hardwareParams.SetRateNear(ref rate, ref dir);
                if(rate != 8000)
                {
                    throw new Exception($"Could not get required sample rate of {8000} instead got {rate}");
                }
                hardwareParams.Channels = 1;
                hardwareParams.SetPeriodSize(160, 0);
                //hardwareParams.SetPeriods(2, 0);

                _soundPcm.HardwareParams = hardwareParams;

                _soundPcm.Prepare();
            }
        }

        public void ReadAudio(short[] buffer)
        {
            int ammountRead = _soundPcm.ReadInterleaved(buffer, 160);
            if(ammountRead != 160)
            {
                Console.Error.WriteLine("failed to get 160 frames of audio");
            }
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        protected void Dispose(bool disposing)
        {
            if(disposing)
            {
                _soundPcm.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}