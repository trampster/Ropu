using System;

namespace Ropu.Client.Alsa
{
    public class AlsaAudioPlayer : IAudioPlayer
    {
        readonly SoundPcm _soundPcm;

        public AlsaAudioPlayer()
        {
            _soundPcm = new SoundPcm("default", snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0);
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

                uint periods = 2;
                hardwareParams.SetPeriodsNear(ref periods, ref dir);
                if(periods != 2)
                {
                    Console.WriteLine($"Requested Alsa Periods 2 but got {periods}");
                }

                _soundPcm.HardwareParams = hardwareParams;
            }

            _soundPcm.Prepare();
            _soundPcm.Start();
        }

        public void PlayAudio(short[] buffer)
        {
            if(160 != _soundPcm.WriteInterleaved(buffer, 160))
            {
                Console.Error.WriteLine("failed to write 160 frames of audio");
            }
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