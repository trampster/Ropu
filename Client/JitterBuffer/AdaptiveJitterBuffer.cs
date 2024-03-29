using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;

namespace Ropu.Client.JitterBuffer
{
    public class AdaptiveJitterBuffer : IJitterBuffer
    {
        BufferEntry[] _buffer;
        int _readIndex = 0;
        int _writeIndex;
        ushort _nextExpectedSequenceNumber = 0;
        uint _currentUserId = 0;
        const float _packetSuccessRequired = 0.95f;
        int _bufferSize;
        readonly int _min;
        readonly int _max;

        float[] _requiredBufferSizeCounts;
        readonly int[] _expireStats = new int[_packetsToAverageOver];
        int _expireIndex = 0;
        int _packetsAveraged = 0;

        const int _packetsToAverageOver = 5*50;

        int _packetsInBuffer = 0;

        public AdaptiveJitterBuffer(int min, int max)
        {
            Console.WriteLine("JitterBuffer Constructor");
            _buffer = new BufferEntry[max];
            for(int index = 0; index < _buffer.Length; index++)
            {
                _buffer[index] = new BufferEntry(index);
            }
            _bufferSize = min;
            _min = min;
            _max = max;

            _writeIndex = _min;
            
            // InitalizeStats
            _requiredBufferSizeCounts = new float[_max*2];
            
            //will ensure, buffer size doesn't change instantly
            for(int index = 0; index < _expireStats.Length; index++)
            {
                _requiredBufferSizeCounts[_min-1] += 1;
                _expireStats[index] = _min -1;
            }
        }

        int GetIndexFromOffset(int offset)
        {
            int newIndex = _writeIndex + offset;
            if(newIndex >= 0)
            {
                return newIndex % _buffer.Length;
            }
            while(newIndex < 0)
            {
                newIndex += _buffer.Length;
            }
            return newIndex;
        }

        int _overId = 0;//incremented for each over we receive
        public void AddAudio(uint userId, ushort sequenceNumber, Span<byte> audioData)
        {
            if(_talker != userId)
            {
                return; //this user doesn't have floor
            }
            lock(_lock)
            {
                if(userId != _currentUserId)
                {
                    _overId++;
                    //receiving from a different user so reset the sequenceNumber
                    _nextExpectedSequenceNumber = sequenceNumber;
                    _currentUserId = userId;
                }
                else
                {
                    short limit = (short)(_nextExpectedSequenceNumber + 900);
                    if(sequenceNumber > limit)
                    {
                        //new over same user
                        _overId++;
                        _nextExpectedSequenceNumber = sequenceNumber;
                    }
                }

                int offset = sequenceNumber - _nextExpectedSequenceNumber;

                int index = GetIndexFromOffset(offset);
                RecordRequiredBufferSize(-1 *offset);


                int maxNegitiveOffset = GetMaxNegativeOffset();
                int maxPostiveOffset = maxNegitiveOffset + _buffer.Length;
                if(offset < maxNegitiveOffset || //packet is to late and we already played out it's spot
                    offset >= maxPostiveOffset) // buffer is not large enough to store this one...
                {
                    Console.WriteLine($"Packet is to late or to early offset {offset}");
                    return;
                }
                if(_buffer[index].IsSet && _buffer[index].OverId == _overId)
                {
                    Console.WriteLine("Attempted to write packet to index that is still set");
                    Console.WriteLine($"index {index}");
                    Console.WriteLine($"sequenceNumber {sequenceNumber}");
                    Console.WriteLine($"_nextExpectedSequenceNumber {_nextExpectedSequenceNumber}");
                    Console.WriteLine($"offset {offset}");
                    Console.WriteLine($"_readIndex {_readIndex}");
                    Console.WriteLine($"_writeIndex {_writeIndex}");
                    Console.WriteLine($"Existing packet has seq num {_buffer[index].SequenceNumber}");

                    throw new Exception($"Attempted to write packet to index that is still set {index} seq {sequenceNumber}");
                }

                _buffer[index].Fill(userId, sequenceNumber, audioData, _overId);
                if(Interlocked.Increment(ref _packetsInBuffer) == 1)
                {
                    _dataInBuffer.Set();
                }
            }
        }

        readonly ManualResetEvent _dataInBuffer = new ManualResetEvent(false);

        void RecordRequiredBufferSize(int requiredBufferSize)
        {
            if(requiredBufferSize > _requiredBufferSizeCounts.Length -1)
            {
                requiredBufferSize = _requiredBufferSizeCounts.Length -1;
            }
            if(requiredBufferSize < 0)
            {
                requiredBufferSize = 0;
            }

            //expire the oldest stat
            var index = _expireStats[_expireIndex];
            _requiredBufferSizeCounts[index]--;

            //record our entry so we can expire it later
            _expireStats[_expireIndex] = requiredBufferSize;

            //increment with wrap
            _expireIndex = (_expireIndex + 1) % _expireStats.Length;
            
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
                Console.WriteLine($"Increasing buffersize to {_bufferSize}");

                return;
            }
            if(_bufferSize == _min)
            {
                return;
            }
            //buffer is to large, need to skip a packet
            if(_buffer[_readIndex].IsSet)
            {
                _buffer[_readIndex].Empty();
                DecrementPacketsInBuffer();
            }
            IncrementWithWrap(ref _readIndex);
            _bufferSize--;
            Console.WriteLine($"Decrease buffersize to {_bufferSize}.");
        }

        readonly object _lock = new object();
        //when the buffer is empty allow the read to continue for another loop through the buffer
        //incase the stream isn't finished just lost for a bit
        int _emptyCount = int.MaxValue;

        uint? _talker = null;
        public uint? Talker 
        { 
            set
            {
                _talker = value;
            }
        }

        bool _returnedAudioSinceEmpty = false;

        public (AudioData?, bool) GetNext(Action streamFinished, Action streamStart)
        {
            if(_packetsInBuffer == 0)
            {
                if(_emptyCount >= _buffer.Length)
                {
                    Console.WriteLine("Giving up on stream, starting wait for new packet.");
                    _currentUserId = 0;
                    _nextExpectedSequenceNumber = 0;
                    streamFinished();
                    _dataInBuffer.WaitOne();
                    _emptyCount = 0;
                    _returnedAudioSinceEmpty = false;
                    streamStart();
                }

                _emptyCount++;
            }
            else
            {
                _emptyCount = 0;
            }
            lock(_lock)
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
                    entry.Empty();
                    DecrementPacketsInBuffer();
                    _returnedAudioSinceEmpty = true;
                    return (audioData, false);
                }

                var nextEntry = _buffer[_readIndex];
                if(_returnedAudioSinceEmpty && nextEntry.IsSet)
                {
                    return (nextEntry.AudioData, true);
                }
                return (null, false);
            }
        }

        void DecrementPacketsInBuffer()
        {
            //Console.WriteLine("Decrement _packetsInBuffer");
            if(Interlocked.Decrement(ref _packetsInBuffer) == 0)
            {
                _dataInBuffer.Reset();
            }
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
            return _bufferSize * -1;
        }
    }
}