using System;

namespace Ropu.Client
{
    public class BufferEntry
    {
        readonly object _lock = new object();

        public uint UserId
        {
            get;
            private set;
        }

        public ushort SequenceNumber
        {
            get;
            private set;
        }

        public Memory<ushort> AudioData
        {
            get;
            private set;
        }

        public bool IsSet
        {
            get;
            private set;
        }

        public void Empty()
        {
            IsSet = false;
        }

        public void Fill(uint userId, ushort sequenceNumber, Memory<ushort> audioData)
        {
            lock(_lock)
            {
                UserId = userId;
                SequenceNumber = sequenceNumber;
                AudioData = audioData;
                IsSet = true;
            }
        }
    }
    public class JitterBuffer
    {

        BufferEntry[] _buffer;
        int _readIndex = 0;
        int _writeIndex = 0;
        ushort _nextSequenceNumber = 0;
        uint _currentUserId = 0;
        Memory<ushort>? _lastAudioData;
        readonly Memory<ushort> _silence = new Memory<ushort>(new ushort[160]);
        float _packetSuccessFraction;
        const float _packetSuccessRequired = 0.95f;
        int _bufferSize;
        readonly int _min;

        public JitterBuffer(int min, int max)
        {
            _buffer = new BufferEntry[max];
            _bufferSize = min;
            _min = min;
            ResetSuccessRateStats();
        }

        void AddAudio(uint userId, ushort sequenceNumber, Memory<ushort> audioData)
        {
            if(userId != _currentUserId)
            {
                //receiving from a different user so reset the sequenceNumber
                _nextSequenceNumber = sequenceNumber;
                _currentUserId = userId;
            }

            int offset = sequenceNumber - _nextSequenceNumber;
            int index = (_writeIndex + offset) % _buffer.Length;


            int maxNegitiveOffset = GetMaxNegativeOffset();
            int maxPostiveOffset = maxNegitiveOffset + _buffer.Length;
            if(offset < maxNegitiveOffset || //packet is to late and we already played out it's spot
               offset > maxPostiveOffset) // packet is to far in the future
            {
                RecordMiss();
                return;
            }

            _buffer[index].Fill(userId, sequenceNumber, audioData);
        }

        Memory<ushort> GetNext()
        {
            var entry = _buffer[_readIndex];
            if(entry.IsSet)
            {
                _readIndex++;
                var audioData = entry.AudioData;
                _lastAudioData = audioData;
                entry.Empty();
                RecordHit();
                if(_packetSuccessFraction > 0.99f)
                {
                    //buffer could be smaller, we can do this by skipping a packet
                    _readIndex++;
                    ResetSuccessRateStats();
                }
                return audioData;
            }
            // we don't record a miss here, we only record packets that actually arrive
            // as misses, this is because we don't want dropped packets, or end of streams to
            // effect our stats and thus change the buffer size.

            if(_packetSuccessFraction < _packetSuccessRequired && _bufferSize < _buffer.Length)
            {
                //by not incrementing the read index, we effectively increase the buffer size
                _bufferSize++;
                ResetSuccessRateStats();
            }
            else
            {
                _readIndex++;
            }

            if(_lastAudioData == null)
            {
                _lastAudioData = null; //only use this once, after that silence
                return _lastAudioData.Value;
            }
            return _silence;
        }
        int _packetsAveraged = 0;
        const int _packetsToAverageOver = 5*50;
        void RecordMiss()
        {
            _packetSuccessFraction = _packetSuccessFraction - 
                (_packetSuccessFraction/_packetsAveraged);

            if(_packetsAveraged < _packetsToAverageOver)
            {
                _packetsAveraged++;
            }
        }

        void RecordHit()
        {
            _packetSuccessFraction = _packetSuccessFraction - 
                (_packetSuccessFraction/_packetsAveraged) + 
                1/_packetsAveraged;
            if(_packetsAveraged < _packetsToAverageOver)
            {
                _packetsAveraged++;
            }
        }

        void ResetSuccessRateStats()
        {
            _packetSuccessFraction = (1-_packetSuccessRequired)/2;
        }

        int GetMaxNegativeOffset()
        {
            if(_writeIndex >= _readIndex)
            {
                return -1*(_writeIndex - _readIndex);
            }
            return -1*(_buffer.Length + _writeIndex - _readIndex);
        }
    }
}