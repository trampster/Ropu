
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

    public class SetChange<T> where T : class
    {
        public ChangeType ChangeType
        {
            get;
            set;
        }

        T? _value;

        public T Value
        {
            get
            {
                if(_value == null)
                {
                    throw new Exception("SetChange is currently cleared");
                }
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public void Clear()
        {
            _value = null;
        }
    }

    public class SnapshotSet<T> where T : class
    {
        T?[] _array;
        int _length;
        readonly int _maxElements;
        readonly List<SetChange<T>> _queuedChanges = new List<SetChange<T>>();

        readonly MemoryPool<SetChange<T>> _setChangePool;

        readonly object _writeLock = new object();

        readonly Dictionary<T, int> _indexLookup;

        volatile bool _locked = false;


        public SnapshotSet(int maxElements)
        {
            _setChangePool = new MemoryPool<SetChange<T>>(() => new SetChange<T>());
            _maxElements = maxElements;
            _indexLookup = new Dictionary<T, int>(maxElements);

            _array = new T[4];
        }

        public void Add(T item)
        {
            lock(_writeLock)
            {
                if(_locked)
                {
                    var setChange = _setChangePool.Get();
                    setChange.ChangeType = ChangeType.Add;
                    setChange.Value = item;
                    _queuedChanges.Add(setChange);
                    return;
                }
                //check if we already have it
                if(_indexLookup.ContainsKey(item))
                {
                    return; 
                }

                AddUnsafe(item);

                _indexLookup.Add(item, _length -1);
            }
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
                var newArray = new T?[newLength];
                for(int index = 0; index < _array.Length; index++)
                {
                    newArray[index] = _array[index];
                }
                _array = newArray;
            }
            _array[_length] = item;
            _length++;
        }

        public void Remove(T item)
        {
            lock(_writeLock)
            {
                if(_locked)
                {
                    var setChange = _setChangePool.Get();
                    setChange.ChangeType = ChangeType.Remove;
                    setChange.Value = item;
                    _queuedChanges.Add(setChange);
                    return;
                }
                if(!_indexLookup.TryGetValue(item, out int itemIndex))
                {
                    return;
                }
                if(itemIndex != _length -1)
                {
                    var movedItem = _array[_length-1];
                    if(movedItem == null) throw new Exception("Logic error moved item should never be null");
                    _indexLookup[movedItem] = itemIndex; //the last item was moved to the index of the old one.
                }
                _indexLookup.Remove(item);
                RemoveUnsafe(itemIndex);

            }
        }

        void RemoveUnsafe(int index)
        {
            _array[index] = _array[_length -1];
            _array[_length - 1] = default(T);
            _length--;
        }

        public Span<T> GetSnapShot()
        {
            _locked = true;
            #nullable disable // should only be null outside the range of the span
            return _array.AsSpan(0, _length);
            #nullable enable

        }

        void ProcessQueuedChanges()
        {
            lock(_writeLock)
            {
                _locked = false;

                foreach(var change in _queuedChanges)
                {
                    if(change.ChangeType == ChangeType.Add)
                    {
                        Add(change.Value);
                    }
                    else
                    {
                        Remove(change.Value);
                    }
                }
                _queuedChanges.Clear();
            }
        }

        public void Release()
        {
            ProcessQueuedChanges();
        }
    }
}