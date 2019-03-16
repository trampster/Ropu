using System;

namespace Ropu.Client.JitterBuffer
{
public class BufferEntry
    {
        readonly object _bufferLock = new object();
        readonly int _index;
        public BufferEntry(int index)
        {
            _index = index;
            AudioData = new AudioData();
        }

        public int Index => _index;

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

        public AudioData AudioData
        {
            get;
            private set;
        }

        public int OverId
        {
            get;
            private set;
        }

        volatile bool _isSet = false;

        public bool IsSet
        {
            get => _isSet;
        }

        public void Empty()
        {
            lock(_bufferLock)
            {
                _isSet = false;
            }
        }

        public void Fill(uint userId, ushort sequenceNumber, Span<byte> audioData, int overId)
        {
            lock(_bufferLock)
            {
                UserId = userId;
                SequenceNumber = sequenceNumber;
                AudioData.Data = audioData;
                OverId = overId;
                _isSet = true;
            }
        }
    }
}