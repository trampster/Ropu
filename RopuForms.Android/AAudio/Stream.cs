using System;

namespace RopuForms.Droid.AAudio
{
    public class Stream
    {
        readonly IntPtr _streamPtr;
        public Stream(IntPtr streamPtr)
        {
            _streamPtr = streamPtr;
        }
    }
}