using System;
using System.Diagnostics;
using System.IO;

namespace Ropu.Client.FileAudio
{
    public class FileAudioSource : IAudioSource
    {
        readonly FileStream _fileStream;
        readonly Stopwatch _stopwatch = new Stopwatch();
        readonly byte[] _buffer = new byte[320];
        int _nextPlayTime;
        public FileAudioSource(string path)
        {
            _fileStream = File.OpenRead(path);
            SkipWavHeader();
            _stopwatch.Start();
        }

        void SkipWavHeader()
        {
            _fileStream.Seek(44, SeekOrigin.Begin);
        }

        public void ReadAudio(short[] output)
        {
            if(_nextPlayTime < _stopwatch.ElapsedMilliseconds - 500)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
                _nextPlayTime = 20;
            }
            int ammountRead = _fileStream.Read(_buffer, 0, 320);
            if(ammountRead == 0)
            {
                //go back to start
                SkipWavHeader();
                _fileStream.Read(_buffer, 0, 320);
            }
            int outputIndex = 0;
            for(int index = 0; index < _buffer.Length; index += 2)
            {
                var value = BitConverter.ToInt16(_buffer, index);
                output[outputIndex] = (short)value;
                outputIndex++;
            }
            
            int waitTime = _nextPlayTime - (int)_stopwatch.ElapsedMilliseconds;
            if(waitTime < 0)
            {
                Console.WriteLine($"Sleep time less than zero {waitTime}");
                waitTime = 0;
            }

            System.Threading.Thread.Sleep(waitTime);

            _nextPlayTime += 20;
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
                _fileStream.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}