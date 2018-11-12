using System;
using System.Net;

namespace Ropu.Shared.Concurrent
{
    /// <summary>
    /// A thread safe set of unordered arrays, 
    /// Optimized for iterating the array, but order N for add or remove.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Set<T> where T : class
    {
        MemoryPool<ReusableMemory<T>> _pool;

        readonly int _max = 100;
        public T[] _buildingSet;
        public int _length = 0;
        public ReusableMemory<T> _safeSet;

        public readonly object _lock = new object();

        bool _refresh = true;

        public Set(int max)
        {
            _max = max;
            _pool = new MemoryPool<ReusableMemory<T>>(() => new ReusableMemory<T>(new T[max]));
            _safeSet = _pool.Get();
            _buildingSet = new T[_max];
        }

        public void Add(T value)
        {
            lock(_lock)
            {
                _buildingSet[_length] = value;
                _length++;
                Refresh();
            }
        }

        public void SuspendRefresh()
        {
            _refresh = false;
        }

        public void ResumeRefresh()
        {
            _refresh = true;
            Refresh();
        }

        void Refresh()
        {
            if(!_refresh)
            {
                return;
            }
            lock(_lock)
            {
                var safe = _pool.Get();
                for(int index = 0; index < _length; index++)
                {
                    safe.Memory[index] = _buildingSet[index];
                }
                safe.SetLength(_length);
                _safeSet = safe;
            }
        }

        public ReusableMemory<T> Get()
        {
            _safeSet.Use();
            return _safeSet;
        }

        int Find(IPEndPoint endPoint)
        {
            for(int index = 0; index < _length; index++)
            {
                if(_buildingSet[index] == endPoint)
                {
                    return index;
                }
            }
            return -1;
        }

        public void Remove(T toRemove)
        {
            lock(_lock)
            {
                var safe = _pool.Get();
                var safeMemory = safe.Memory;
                bool foundIt = false;
                for(int index = 0; index < _length -1; index++)
                {
                    var endPoint = _buildingSet[index];
                    if(endPoint == toRemove)
                    {
                        foundIt = true;
                        endPoint = _buildingSet[_length -1];
                        _buildingSet[index] = endPoint;
                    }
                    safeMemory[index] = endPoint;
                }
                if(foundIt)
                {
                    _length--;
                    safe.SetLength(_length);
                    _safeSet = safe;
                }
            }
        }

        public void Recycle(ReusableMemory<T> used)
        {
            if(used.Free())
            {
                _pool.Add(used);
            }
        }
    }
}
