using System;
using System.Collections.Generic;

namespace Ropu.ControllingFunction.FileServer
{
    public class FilePart
    {
        int _length;
        readonly byte[] _buffer;


        public FilePart(byte[] buffer)
        {
            _buffer = buffer;
        }

        public void SetLength(int length)
        {
            _length = length;
        }

        public void Reset()
        {
            _length = _buffer.Length;
        }

        public byte[] Buffer => _buffer;
    }

}