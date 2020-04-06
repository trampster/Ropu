using System;
using System.Threading.Tasks;
using Ropu.Client;
using Xamarin.Essentials;

namespace RopuForms.Droid.AAudio
{
    public class AAudioSource : IAudioSource
    {
        Stream _stream;
        readonly DownSampler _downSampler;

        public AAudioSource()
        {
            _downSampler = new DownSampler();
        }

        void OpenStream()
        {
            using (var streamBuilder = new StreamBuilder())
            {
                _numFrames = 160 * 6;
                streamBuilder.ChannelCount = 1;
                streamBuilder.ContentType = ContentType.Speech;
                streamBuilder.Direction = Direction.Input;
                streamBuilder.ErrorCallback = StreamError;
                streamBuilder.Format = Format.PcmI16;
                streamBuilder.InputPreset = InputPreset.VoiceRecognition; // often provides the lowest latency
                streamBuilder.NumFrames = _numFrames * 2;
                streamBuilder.PerformanceMode = PerformanceMode.LowLatency; //might need to change this if in background mode
                streamBuilder.SampleRate = 48000;
                streamBuilder.Usage = Usage.VoiceCommunication;
                var result = streamBuilder.OpenStream(out _stream);
                if(result != Result.OK)
                {
                    Console.Error.WriteLine($"Failed to create stream {result}");
                    return;
                }
                _stream.RequestStart();
            }

            Console.WriteLine($"AAudio Stream created {_stream.SampleRate} State {_stream.State}");
        }

        void StreamError(Result result)
        {
            Console.Error.WriteLine($"AAudioSource stream error {result}, restarting...");
            Task.Run(() => OpenStream());
        }

        short[] _buffer = new short[160 * 6];
        int _numFrames;

        public void ReadAudio(short[] buffer)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("You must call start before ReadAudio");
            }

            var result = (int)_stream.Read(_buffer, _numFrames, 10000*1000000L);
            if(result == _numFrames)
            {
                Console.WriteLine("Read success");
                try
                {
                    _downSampler.DownSample(_buffer, buffer);
                }
                catch(Exception exception)
                {
                    Console.WriteLine($"DownSample threw exception {exception}");
                    throw;
                }
                return;
            }
            if(result < 0)
            {
                Console.Error.WriteLine($"Failed to read audio with result {(Result)(result)}");
                return;
            }
            if(result >= 0)
            {
                Console.Error.WriteLine($"Unexpected ammount of samples read expected 960 but got {result}, State {_stream.State}");
            }
        }

        public void Start()
        {
            if (_stream == null)
            {
                OpenStream();
            }
        }

        public void Stop()
        {
            _stream?.Dispose();
            _stream = null;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}