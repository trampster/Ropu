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
        int _writeIndex;
        ushort _nextExpectedSequenceNumber = 0;
        uint _currentUserId = 0;
        Memory<ushort>? _lastAudioData;
        readonly Memory<ushort> _silence = new Memory<ushort>(new ushort[160]);
        const float _packetSuccessRequired = 0.95f;
        int _bufferSize;
        readonly int _min;
        readonly int _max;

        float[] _requiredBufferSizeCounts;
        readonly int[] _expireStats = new int[_packetsToAverageOver];
        int _expireIndex = 0;
        int _packetsAveraged = 0;

        const int _packetsToAverageOver = 5*50;

        public JitterBuffer(int min, int max)
        {
            _buffer = new BufferEntry[max];
            for(int index = 0; index < _buffer.Length; index++)
            {
                _buffer[index] = new BufferEntry();
            }
            _bufferSize = min;
            _min = min;
            _max = max;

            _writeIndex = _min - 1;
            IntializeStats();
        }

        void IntializeStats()
        {
            _requiredBufferSizeCounts = new float[_max*2];
            
            //will ensure, buffer size doesn't change instantly
            for(int index = 0; index < _expireStats.Length; index++)
            {
                _requiredBufferSizeCounts[_min-1] += 1;
                _expireStats[index] = _min -1;
            }

        }

        public void AddAudio(uint userId, ushort sequenceNumber, Memory<ushort> audioData)
        {
            if(userId != _currentUserId)
            {
                //receiving from a different user so reset the sequenceNumber
                _nextExpectedSequenceNumber = sequenceNumber;
                _currentUserId = userId;
            }

            int offset = sequenceNumber - _nextExpectedSequenceNumber;

            int index = (_writeIndex + offset) % _buffer.Length;
            RecordRequiredBufferSize(_bufferSize - distanceAheadOfReadIndex(index));


            int maxNegitiveOffset = GetMaxNegativeOffset();
            int maxPostiveOffset = maxNegitiveOffset + _buffer.Length;
            if(offset < maxNegitiveOffset || //packet is to late and we already played out it's spot
               offset > maxPostiveOffset) // buffer is not large enough to store this one...
            {
                return;
            }

            _buffer[index].Fill(userId, sequenceNumber, audioData);
        }

        int distanceAheadOfReadIndex(int index)
        {
            var diff = index - _readIndex;
            if(diff >= 0)
            {
                return diff;
            }
            return _buffer.Length - (diff*-1);
        }

        void RecordRequiredBufferSize(int requiredBufferSize)
        {
            //expire the oldest stat
            var index = _expireStats[_expireIndex];
            _requiredBufferSizeCounts[index]--;

            //record out entry so we can expire it later
            _expireStats[_expireIndex] = requiredBufferSize;

            //increment with wrap
            _expireIndex = (_expireIndex + 1) % _expireStats.Length;
            

            if(requiredBufferSize > _requiredBufferSizeCounts.Length -1)
            {
                requiredBufferSize = _requiredBufferSizeCounts.Length -1;
            }
            _requiredBufferSizeCounts[requiredBufferSize] += 1;

            _packetsAveraged++;
        }

        int CalulateIdealBufferSize()
        {
            //find the total
            float total = 0;
            for(int index = 0; index < _requiredBufferSizeCounts.Length; index++)
            {
                var count = _requiredBufferSizeCounts[index];
                total += count;
            }

            float required = total*_packetSuccessRequired;
            float soFar = 0;


            for(int index = 0; index < _requiredBufferSizeCounts.Length; index++)
            {
                soFar += _requiredBufferSizeCounts[index];
                if(soFar > required)
                {
                    return index + 1;
                }
            }
            return _requiredBufferSizeCounts.Length;
        }

        void UpdateBufferSize()
        {
            var idealBufferSize = CalulateIdealBufferSize();
            if(_bufferSize == idealBufferSize)
            {
                return;
            } 
            if(_bufferSize < idealBufferSize)
            {
                DecrementWithWrap(ref _readIndex);
                _bufferSize++;
                return;
            }
            //buffer is to large, need to skip a packet
            _buffer[_readIndex].Empty();
            IncrementWithWrap(ref _readIndex);
            _bufferSize--;
        }

        public Memory<ushort> GetNext()
        {
            //This should get called every 20 milliseconds, so we use this to increment the _writeIndex
            IncrementWithWrap(ref _writeIndex);
            _nextExpectedSequenceNumber++;

            var entry = _buffer[_readIndex];
            IncrementWithWrap(ref _readIndex);
            UpdateBufferSize();

            if(entry.IsSet)
            {
                //success, the packet is available
                var audioData = entry.AudioData;
                _lastAudioData = audioData;
                entry.Empty();
                return audioData;
            }
            // Was a miss (either late or lost)
            if(_lastAudioData != null)
            {
                var last = _lastAudioData.Value;
                _lastAudioData = null; //only use this once, after that silence
                return last;
            }
            return _silence;
        }

        void IncrementWithWrap(ref int index)
        {
            index++;
            if(index == _buffer.Length)
            {
                index = 0;
            }
        }

        void DecrementWithWrap(ref int index)
        {
            index--;
            if(index < 0)
            {
                index = _buffer.Length - 1;
            }
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