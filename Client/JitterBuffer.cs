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

        float[] _requiredBufferSizeCounts;
        int _packetsAveraged = 0;

        const int _packetsToAverageOver = 5*50;

        public JitterBuffer(int min, int max)
        {
            _buffer = new BufferEntry[max];
            _bufferSize = min;
            _min = min;
            _requiredBufferSizeCounts = new float[max*2];
            _writeIndex = _min;
        }

        void AddAudio(uint userId, ushort sequenceNumber, Memory<ushort> audioData)
        {
            if(userId != _currentUserId)
            {
                //receiving from a different user so reset the sequenceNumber
                _nextExpectedSequenceNumber = sequenceNumber;
                _currentUserId = userId;
            }

            int offset = sequenceNumber - _nextExpectedSequenceNumber;

            int index = (_writeIndex + offset) % _buffer.Length;
            RecordRequiredBufferSize(_bufferSize - (index - _readIndex));


            int maxNegitiveOffset = GetMaxNegativeOffset();
            int maxPostiveOffset = maxNegitiveOffset + _buffer.Length;
            if(offset < maxNegitiveOffset || //packet is to late and we already played out it's spot
               offset > maxPostiveOffset) // buffer is not large enough to store this one...
            {
                return;
            }

            _buffer[index].Fill(userId, sequenceNumber, audioData);
        }

        void RecordRequiredBufferSize(int requiredBufferSize)
        {
            float reduceAmount = 1/_packetsToAverageOver;

            for(int index = 0; index < _requiredBufferSizeCounts.Length; index++)
            {
                float hitCount = _requiredBufferSizeCounts[index];
                if(hitCount > reduceAmount)
                {
                    _requiredBufferSizeCounts[index] = hitCount - reduceAmount;
                    continue;
                }
                _requiredBufferSizeCounts[index] = 0;
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
                _readIndex--;
                _bufferSize++;
                return;
            }
            //buffer is to large, need to skip a packet
            _buffer[_readIndex].Empty();
            _readIndex++;
            _bufferSize--;
        }

        Memory<ushort> GetNext()
        {
            //This should get called every 20 milliseconds, so we use this to increment the _writeIndex
            _writeIndex++;
            _nextExpectedSequenceNumber++;

            var entry = _buffer[_readIndex];
            _readIndex++;
            UpdateBufferSize();

            if(entry.IsSet)
            {
                //success, the packet is available
                _readIndex++;
                var audioData = entry.AudioData;
                _lastAudioData = audioData;
                entry.Empty();
                return audioData;
            }
            // Was a miss (either late or lost)
            if(_lastAudioData == null)
            {
                _lastAudioData = null; //only use this once, after that silence
                return _lastAudioData.Value;
            }
            return _silence;
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