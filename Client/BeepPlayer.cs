using System;

namespace Ropu.Client
{
    public class BeepPlayer : IDisposable
    {
        readonly IAudioPlayer _audioPlayer;

        public BeepPlayer(IAudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
        }

        public void PlayGoAhead()
        {
            PlayTone(440, 300);
        }

        public void PlayDenied()
        {
            PlayTone(369.994, 200);
            System.Threading.Thread.Sleep(20);
            PlayTone(329.628, 400);
        }

        void PlayTone(double freq, int duration)
        {
            const int sampleRate = 8000;
            int samplesRequired = (int)(sampleRate * (duration/1000d));
            samplesRequired += samplesRequired % 160;// make it a whole number

            double secondsPerSample = 1d/sampleRate;
            double signFactor = 2*Math.PI*freq*secondsPerSample;
            short[] buffer = new short[samplesRequired];
            for(int index = 0; index < samplesRequired; index++)
            {
                double time = index*secondsPerSample;
                short sample =  (short)(short.MaxValue * Math.Sin(signFactor * index));
                buffer[index] = sample;
            }
            Play(buffer);
        }

        void Play(short[] audio)
        {
            const double attenationFactor = 1d/160d;
            int attenuationIndex = 0;
            for(int index = audio.Length -161; index < audio.Length; index++)
            {
                attenuationIndex++;
                audio[index] = (short) (audio[index] * (1-(attenationFactor*attenuationIndex)));
            }
            short[] buffer = new short[160];
            for(int audioIndex = 0; audioIndex < audio.Length; audioIndex += 160)
            {
                for(int index = 0; index < buffer.Length; index++)
                {
                    buffer[index] = audio[audioIndex + index];
                }
                _audioPlayer.PlayAudio(buffer);
            }
        }

        readonly short[] _silence = new short[160];

        void StopOnZeroCrossing(short[] buffer)
        {
            bool endPositive = buffer[buffer.Length -1] > 0;
            for(int index = buffer.Length -2; index >= 0; index--)
            {
                if(endPositive)
                {
                    if(buffer[index] < 0)
                    {
                        //found zero crossing
                        PadBuffer(index + 1, buffer);//make the rest zeros
                        return;
                    }
                }
                else
                {
                    if(buffer[index] > 0)
                    {
                        //found zero crossing
                        PadBuffer(index + 1, buffer);//make the rest zeros
                        return;
                    }
                }
            }
        }

        void PadBuffer(int start, short[] buffer)
        {
            for(int index = start; index < buffer.Length; index++)
            {
                buffer[index] = 0; //fill the rest with zeros
            }
        }

        protected void Dispose(bool disposing)
        {
            if(disposing)
            {
                _audioPlayer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}