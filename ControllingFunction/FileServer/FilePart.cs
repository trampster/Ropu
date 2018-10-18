using System;
using System.Collections.Generic;

namespace Ropu.ControllingFunction.FileServer
{
    public class FilePart
    {
        readonly byte[] _buffer;


        public FilePart(byte[] buffer)
        {
            _buffer = buffer;
            Length = _buffer.Length;
        }

        public int Length
        {
            get;
            set;
        }

        public void Reset()
        {
            Length = _buffer.Length;
        }

        public ArraySegment<byte> AsArraySegment()
        {
            return new ArraySegment<byte>(_buffer, 0, Length);
        }

        public byte[] Buffer => _buffer;
    }

}