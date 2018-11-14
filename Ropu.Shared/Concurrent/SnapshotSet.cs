
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ropu.Shared.Concurrent
{
    public class SnapshotSet<T>
    {
        readonly List<SpeedReadSet<T>> _sets = new List<SpeedReadSet<T>>();
        SpeedReadSet<T> _current;
        SpeedReadSet<T> _prestine; //this one is never used, so it's array is always upto date and can be used to clone new ones;
        readonly int _maxElements;

        readonly object _writeLock = new object();

        public SnapshotSet(int maxElements)
        {
            _maxElements = maxElements;
            _current = new SpeedReadSet<T>(maxElements);
            _prestine = new SpeedReadSet<T>(maxElements);

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
                newCurrent = new SpeedReadSet<T>(_maxElements, _prestine.GetSpan());
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
                _prestine.Remove(item);
                for(int index = 0; index < _sets.Count; index++)
                {
                    _sets[index].Remove(item);
                }
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
        public T[] _array;
        public int _length;
        public Dictionary<T, int> _indexLookup;

        public SpeedReadSet(int max)
        {
            _array = new T[max];
            _indexLookup = new Dictionary<T, int>(max);
        }

        public SpeedReadSet(int max, Span<T> initialData)
        {
            _array = new T[max];
            _indexLookup = new Dictionary<T, int>(max);

            for(int index = 0; index < initialData.Length; index++)
            {
                var item = initialData[index];
                _array[index] = initialData[index];
                _indexLookup.Add(item, index);
            }
            _length = initialData.Length;
        }

        void AddUnsafe(T item)
        {
            _array[_length] = item;
            _indexLookup.Add(item, _length);
            _length++;
        }

        public void Add(T item)
        {
            if(_users > 0)
            {
                _queuedChanges.Enqueue(() => AddUnsafe(item));
                return;
            }
            AddUnsafe(item);
        }

        void RemoveUnsafe(T item)
        {
            if(!_indexLookup.TryGetValue(item, out int index))
            {
                return;
            }
            _array[index] = _array[_length -1];
            _array[_length - 1] = default(T);
            _indexLookup.Remove(item);
            _length--;
        }

        public void Remove(T item)
        {
            if(_users > 0)
            {
                _queuedChanges.Enqueue(() => RemoveUnsafe(item));
                return;
            }
            RemoveUnsafe(item);
        }

        Queue<Action> _queuedChanges = new Queue<Action>();

        void ProcessQueuedChanges()
        {
            while(_queuedChanges.TryDequeue(out Action action))
            {
                action();
            }
        }

        int _users;
        bool _inUse;
        bool _available; // availble when not in use, all users have returned it and we are upto date with changes;

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
                //safe to modify 
                ProcessQueuedChanges();
                _available = true;
            }
        }

        public bool Available()
        {
            return _available;
        }
    }
}