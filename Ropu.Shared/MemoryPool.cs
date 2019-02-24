using System;
using System.Collections.Generic;
using System.Threading;

namespace Ropu.Shared
{
    public class MemoryPool<T> where T : class
    {
        int _nextAvailable = -1;
        List<T> _availableItems = new List<T>();
        readonly object _lock = new object();

        readonly Func<T> _createNew;

        public MemoryPool(Func<T> createNew)
        {
            _createNew = createNew;
        }

        public T Get()
        {
            T available = null;
            lock(_lock)
            {
                if(_nextAvailable < 0)
                {
                    return _createNew();
                }
                available = _availableItems[_nextAvailable];
                _availableItems[_nextAvailable] = null;
                _nextAvailable--;
            }
            return available;
        }

        public void Add(T item)
        {
            lock(_lock)
            {
                _nextAvailable++;
                if(_nextAvailable >= _availableItems.Count)
                {
                    _availableItems.Add(item);
                    return;
                }
                _availableItems[_nextAvailable] = item;
            }
        }
    }
}