using System;

namespace Ropu.Shared.Concurrent
{
    public class ReusableMemory<T> : IReusableMemory<T>
    {
        int _users = 0;
        int _length = 0;
        T[] _memory;

        public ReusableMemory(T[] memory)
        {
            _memory = memory;
        }

        public void Use()
        {
            _users++;
        }

        public Span<T> AsSpan()
        {
            return _memory.AsSpan(_length);
        }

        public T[] Memory => _memory;

        /// <summary>
        /// Returns true if there are no users
        /// </summary>
        public bool Free()
        {
            _users--;
            return _users == 0;
        }

        public void SetLength(int length)
        {
            _length = length;
        }
    }
}