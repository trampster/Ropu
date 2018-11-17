using System.Collections;
using System.Collections.Generic;

namespace Ropu.Shared.Concurrent
{
    public class IntDictionary<T>
    {
        const int _setSize = 256;
        int _count = 0;

        readonly T[][][] _store;


        public int Count => _count;

        public IntDictionary()
        {
            _store = new T[ushort.MaxValue][][];
        }

        class Cell
        {
            public bool IsValue => Array == null;
            Cell[] Array{get;set;}

            int Value{get;set;}
        }

        public void Add(uint index, T value)
        {
            uint index1 = index >> 16;
            uint index2 = (index >> 8) & 0xFF;
            uint index3 = index & 0xFF;

            var first = _store[index1];
            if(first == null)
            {
                first = new T[_setSize][];
                _store[index1] = first;
            }

            var second = first[index2];
            if(second == null)
            {
                second = new T[_setSize];
                first[index2] = second;
            }

            second[index3] = value;
            _count++;
        }

        public T this[uint index]
        {
            get 
            { 
                uint index1 = index >> 16;
                uint index2 = (index >> 8) & 0xFF;
                uint index3 = index & 0xFF;
                return _store[index1][index2][index3];
            }
            set
            {
                uint index1 = index >> 16;
                uint index2 = (index >> 8) & 0xFF;
                uint index3 = index & 0xFF;
                _store[index1][index2][index3] = value;
            }
        }

        public bool TryGetValue(uint index, out T value)
        {
            uint index1 = index >> 16;
            uint index2 = (index >> 8) & 0xFF;
            uint index3 = index & 0xFF;
            var first = _store[index1];
            if(first == null)
            { 
                value = default(T);
                return false;
            }
            var second = first[index2];
            if(second == null) 
            {
                value = default(T);
                return false;
            }
            value = second[index3];
            if(value.Equals(default(T)))
            {
                //one of the short commings of this dictionary is it can't tell the difference between
                //doesn't contain and default, so assumes default means doesn't contain
                return false;
            }
            return true;
        }

        public void AddOrUpdate(uint index, T value)
        {
            uint index1 = index >> 16;
            uint index2 = (index >> 8) & 0xFF;
            uint index3 = index & 0xFF;

            var first = _store[index1];
            if(first == null)
            {
                first = new T[_setSize][];
                _store[index1] = first;
            }

            var second = first[index2];
            if(second == null)
            {
                second = new T[_setSize];
                first[index2] = second;
            }

            var old = second[index3];
            second[index3] = value;
            if(old.Equals(default(T)))
            {
                _count++;
            }
        }

        public void Remove(uint index)
        {
            uint index1 = index >> 16;
            uint index2 = (index >> 8) & 0xFF;
            uint index3 = index & 0xFF;
            _store[index1][index2][index3] = default(T);
            _count--;
        }

        public bool ContainsKey(uint key)
        {
            throw new System.NotImplementedException();
        }
    }
}