using System;
using System.Linq;
using System.Threading.Tasks;
using Ropu.Client;

namespace RopuForms.Droid.AAudio
{
    public class AAudioPlayer : IAudioPlayer
    {
        Stream _stream;
        readonly Resampler _resampler;
        readonly object _lock = new object();

        public AAudioPlayer(Resampler resampler)
        {
            _resampler = resampler;
        }

        void OpenStream()
        {
            using (var streamBuilder = new StreamBuilder())
            {
                _numFrames = 160 * 6;
                streamBuilder.ChannelCount = 1;
                streamBuilder.ContentType = ContentType.Speech;
                streamBuilder.Direction = Direction.Output;
                streamBuilder.ErrorCallback = StreamError;
                streamBuilder.Format = Format.PcmI16;
                streamBuilder.NumFrames = _numFrames * 2;
                streamBuilder.PerformanceMode = PerformanceMode.LowLatency; //might need to change this if in background mode
                streamBuilder.SampleRate = 48000;
                streamBuilder.Usage = Usage.Media;
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

        public void PlayAudio(short[] buffer)
        {
            lock (_lock)
            {
                if (_stream == null)
                {
                    OpenStream();
                }

                if(_stream.State != StreamState.Started && _stream.State != StreamState.Starting)
                {
                    return;
                }

                _resampler.UpSample(buffer, _buffer);

                var result = (int)_stream.Write(_buffer, _numFrames, 10000 * 1000000L);
                if (result == _numFrames)
                {
                    return;
                }
                if (result < 0)
                {
                    Console.Error.WriteLine($"Failed to write audio with result {(Result)(result)}");
                    return;
                }
                if (result >= 0)
                {
                    Console.Error.WriteLine($"Unexpected ammount of samples written expected 960 but got {result}, State {_stream.State}");
                }
            }
        }


        public void Pause()
        {
            lock (_lock)
            {
                if (_stream != null)
                {
                    var result = _stream.RequestPause();
                    if (result != Result.OK)
                    {
                        Console.Error.WriteLine($"Failed to pause stream");
                    }
                }
            }
        }

        public void Resume()
        {
            lock (_lock)
            {
                if (_stream != null)
                {
                    if(_stream.State != StreamState.Paused && _stream.State != StreamState.Pausing)
                    {
                        return;
                    }
                    var result = _stream.RequestStart();
                    if (result != Result.OK)
                    {
                        Console.Error.WriteLine($"Failed to resume stream");
                    }
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _stream?.Dispose();
                _stream = null;
            }
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