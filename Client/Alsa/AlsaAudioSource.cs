using System;
using System.Diagnostics;

namespace Ropu.Client.Alsa
{

    public class AlsaAudioSource : IAudioSource
    {
        SoundPcm _soundPcm;

        public AlsaAudioSource()
        {

        }

        public void ReadAudio(short[] buffer)
        {
            if(_firstAfterStart)
            {
                //for whatever reason, After the first read there are a while lot of frames suddenly (and without time for them to fill, ie they are created from magic) 
                //becoming available
                //if we don't clear these then we spam out about 18 packets of audio which could overload the jitter buffer at the other end.
                _soundPcm.ReadInterleaved(buffer, 160);

                while(_soundPcm.Available() > 160)
                {
                    Console.WriteLine("to many frames in buffer skipping");
                    _soundPcm.ReadInterleaved(buffer, 160);
                }
                _firstAfterStart = false;
            }

            int ammountRead = _soundPcm.ReadInterleaved(buffer, 160);
            if(ammountRead != 160)
            {
                Console.Error.WriteLine("failed to get 160 frames of audio");
            }
        }

        volatile bool _firstAfterStart = false;

        public void Start()
        {
            _firstAfterStart = true;
            if(_soundPcm != null)
            {
                _soundPcm.Dispose();
            }
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


                uint periods = 2;
                hardwareParams.SetPeriodsNear(ref periods, ref dir);
                if(periods != 2)
                {
                    Console.WriteLine($"Requested Alsa Periods 2 but got {periods}");
                }

                _soundPcm.HardwareParams = hardwareParams;

                _soundPcm.Prepare(); //this starts filling the buffer so need top Drop
                _soundPcm.Start();

            }
        }

        Stopwatch _stopwatch = new Stopwatch();

        public void Stop()
        {
            if(_soundPcm != null)
            {
                _soundPcm.Dispose();
            }
        }

        protected void Dispose(bool disposing)
        {
            if(disposing)
            {
                _soundPcm?.Dispose();
                _soundPcm = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}