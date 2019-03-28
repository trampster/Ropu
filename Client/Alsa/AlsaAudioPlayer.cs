using System;
using System.Diagnostics;
using System.Linq;

namespace Ropu.Client.Alsa
{
    public class AlsaAudioPlayer : IAudioPlayer
    {
        readonly SoundPcm _soundPcm;
        readonly uint _periods; //size of the buffer in periods(160 frames)

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
                    throw new Exception($"AlsaAudioPlayer: Could not get required sample rate of {8000} instead got {rate}");
                }
                hardwareParams.Channels = 1;

                hardwareParams.SetPeriodSize(160, 0);
                uint periods = 2;
                hardwareParams.SetPeriodsNear(ref periods, ref dir);
                if (periods != 2)
                {
                    Console.WriteLine($"AlsaAudioPlayer: Could not get required periods {2} instead got {periods}");
                }
                _periods = periods;
                uint bufferSize = 320;
                hardwareParams.SetBufferNear(ref bufferSize);
                if(bufferSize != 320)
                {
                    Console.WriteLine($"AlsaAudioPlayer: Could not get requested buffer size instead got {bufferSize}");
                }

                _soundPcm.HardwareParams = hardwareParams;
            }
        }

        bool _prepared = false;

        readonly short[] _silence = new short[160];

        void Prepare()
        {
            _soundPcm.Prepare();
            for(int index = 0; index < _periods; index++)
            {
                _soundPcm.WriteInterleaved(_silence, 160);
            }
        }
        public void PlayAudio(short[] buffer)
        {
            if(!_prepared)
            {
                Prepare();
                _prepared = true;
            }
            int result = _soundPcm.WriteInterleaved(buffer, 160);
            if(result == -32)
            {
                Prepare();
                result = _soundPcm.WriteInterleaved(buffer, 160);
            }
            if(160 != result)
            {
                Console.Error.WriteLine($"failed to write 160 frames of audio instead returned {result}");
            }
            return;
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