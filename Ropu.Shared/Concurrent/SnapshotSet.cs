
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ropu.Shared.Concurrent
{

    public enum ChangeType
    {
        Add,
        Remove
    }

    public class SetChange<T>
    {
        public ChangeType ChangeType
        {
            get;
            set;
        }

        public T Value
        {
            get;
            set;
        }

        public int Index
        {
            get;
            set;
        }
    }

    public class SnapshotSet<T>
    {
        readonly List<SpeedReadSet<T>> _sets = new List<SpeedReadSet<T>>();
        SpeedReadSet<T> _current;
        SpeedReadSet<T> _prestine; //this one is never used, so it's array is always upto date and can be used to clone new ones;
        readonly int _maxElements;
        readonly MemoryPool<SetChange<T>> _setChangePool;

        readonly object _writeLock = new object();

        readonly Dictionary<T, int> _indexLookup;

        public SnapshotSet(int maxElements)
        {
            _setChangePool = new MemoryPool<SetChange<T>>(() => new SetChange<T>());
            _maxElements = maxElements;
            _current = new SpeedReadSet<T>(maxElements, _setChangePool);
            _prestine = new SpeedReadSet<T>(maxElements, _setChangePool);
            _indexLookup = new Dictionary<T, int>(maxElements);

            _sets.Add(_current);
        }

        public void Add(T item)
        {
            lock(_writeLock)
            {
                _prestine.Add(item);
                for(int index = 0; index < _sets.Count; index++)
                {
                    _sets[index].Add(item);
                }
                ChangeCurrent();
                _indexLookup.Add(item, _prestine.Length -1);
            }
        }

        void ChangeCurrent()
        {
            //find a new one to use
            SpeedReadSet<T> newCurrent = null;
            for(int index = 0; index < _sets.Count; index++)
            {
                if(_sets[index].Available())
                {
                    newCurrent = _sets[index];
                    newCurrent.Use();
                }
            }
            if(newCurrent == null)
            {
                //we ran out of available sets, so we don't have any that are upto date
                newCurrent = new SpeedReadSet<T>(_maxElements, _prestine.GetSpan(), _setChangePool);
                _sets.Add(newCurrent);
                
            }

            var oldCurrent = _current;
            _current = newCurrent; //as soon as it's assigned it will start being used

            oldCurrent.StopUsing();

        }

        public void Remove(T item)
        {
            lock(_writeLock)
            {
                if(!_indexLookup.TryGetValue(item, out int itemIndex))
                {
                    return;
                }
                var movedItem = _prestine.GetSpan()[_prestine.Length-1];
                _prestine.Remove(itemIndex);
                for(int setIndex = 0; setIndex < _sets.Count; setIndex++)
                {
                    _sets[setIndex].Remove(itemIndex);
                }
                _indexLookup.Remove(item);
                _indexLookup[movedItem] = itemIndex; //the last item was moved to the index of the old one.
                ChangeCurrent();
            }
        }

        public ISetReader<T> GetSnapShot()
        {
            return _current;
        }
    }

    public interface ISetReader<T>
    {
        /// <summary>
        /// Gets a fixed in time view of the set, 
        /// You must call Release when you have finished
        /// </summary>
        Memory<T> GetMemory();

        /// <summary>
        /// Gets a fixed in time view of the set, 
        /// You must call Release when you have finished
        /// </summary>
        Span<T> GetSpan();

        /// <summary>
        /// Must be called when you have finished reading, so the set memory can be recycled
        /// </summary>
        void Release();
    }

    public class SpeedReadSet<T> : ISetReader<T>
    {
        T[] _array;
        int _length;
        readonly MemoryPool<SetChange<T>> _setChangePool;
        readonly int _maxElements;

        public SpeedReadSet(int maxElements, MemoryPool<SetChange<T>> setChangePool)
        {
            _array = new T[4];
            _setChangePool = setChangePool;
        }

        public int Length => _length;

        public SpeedReadSet(int max, Span<T> initialData, MemoryPool<SetChange<T>> setChangePool)
        {
            _array = new T[max];

            for(int index = 0; index < initialData.Length; index++)
            {
                _array[index] = initialData[index];
            }
            _length = initialData.Length;
            _setChangePool = setChangePool;
        }

        void AddUnsafe(T item)
        {
            if(_length >= _array.Length)
            {
                int newLength = _array.Length *2;
                if(newLength > _maxElements)
                {
                    newLength = _maxElements;
                }
                _array = new T[newLength];
            }
            _array[_length] = item;
            _length++;
        }

        public void Add(T item)
        {
            if(_needsProcessing)
            {
                ProcessQueuedChanges();
            }
            if(_users > 0)
            {
                var setChange = _setChangePool.Get();
                setChange.ChangeType = ChangeType.Add;
                setChange.Value = item;
                _queuedChanges.Add(setChange);
                return;
            }
            AddUnsafe(item);
        }

        void RemoveUnsafe(int index)
        {
            _array[index] = _array[_length -1];
            _array[_length - 1] = default(T);
            _length--;
        }

        public void Remove(int index)
        {
            if(_needsProcessing)
            {
                ProcessQueuedChanges();
            }
            if(_users > 0)
            {
                var setChange = _setChangePool.Get();
                setChange.ChangeType = ChangeType.Remove;
                setChange.Index = 0;
                _queuedChanges.Add(setChange);
                return;
            }
            RemoveUnsafe(index);
        }

        List<SetChange<T>> _queuedChanges = new List<SetChange<T>>();

        void ProcessQueuedChanges()
        {
            foreach(var change in _queuedChanges)
            {
                if(change.ChangeType == ChangeType.Add)
                {
                    AddUnsafe(change.Value);
                }
                else
                {
                    RemoveUnsafe(change.Index);
                }
            }
            _queuedChanges.Clear();
            _needsProcessing = false;
            _available = true;
        }

        int _users;
        bool _inUse;
        bool _available; // availble when not in use, all users have returned it and we are upto date with changes;
        volatile bool _needsProcessing = false;

        public void Use()
        {
            _inUse = true;
            _available = false;
        }

        public void StopUsing()
        {
            _inUse = false;
        }

        public Span<T> GetSpan()
        {
            Interlocked.Increment(ref _users);
            return _array.AsSpan(0, _length);
        }

        public Memory<T> GetMemory()
        {
            Interlocked.Increment(ref _users);
            return _array.AsMemory(0, _length);
        }

        public void Release()
        {
            Interlocked.Decrement(ref _users);
            if(_users == 0 && !_inUse)
            {
                _needsProcessing = true;
            }
        }

        public bool Available()
        {
            return _available;
        }
    }
}