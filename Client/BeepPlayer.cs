using System;
using System.Threading.Tasks;

namespace Ropu.Client
{
    public class BeepPlayer : IBeepPlayer
    {
        readonly short[] _goAhead;
        readonly short[] _deniedPartOne;
        readonly short[] _deniedPartTwo;
        readonly short[] _buffer = new short[160];
        readonly object _lock = new object();

        readonly IAudioPlayer _audioPlayer;

        public BeepPlayer(IAudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
            _goAhead = BuildTone(440,300);
            _deniedPartOne = BuildTone(369.994, 200);
            _deniedPartTwo = BuildTone(329.628, 400);
        }

        public void PlayGoAhead()
        {
            Task.Run(() =>  
            {
                lock(_lock)
                {
                    Play(_goAhead);
                }
            });
        }

        public void PlayDenied()
        {
            Task.Run(() =>
            {
                lock(_lock)
                {
                    Play(_deniedPartOne);
                    System.Threading.Thread.Sleep(20);
                    Play(_deniedPartTwo);
                }
            });
        }

        static readonly short _max  = (short)(short.MaxValue * 0.9);

        short[] BuildTone(double freq, int duration)
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
                short sample =  (short)(_max * Math.Sin(signFactor * index));
                buffer[index] = sample;
            }
            return buffer;
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
            for(int audioIndex = 0; audioIndex < audio.Length; audioIndex += 160)
            {
                for(int index = 0; index < _buffer.Length; index++)
                {
                    _buffer[index] = audio[audioIndex + index];
                }
                _audioPlayer.PlayAudio(_buffer);
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