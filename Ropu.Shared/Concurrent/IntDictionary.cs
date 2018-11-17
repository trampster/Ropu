using System.Collections;
using System.Collections.Generic;

namespace Ropu.Shared.Concurrent
{
    public class IntDictionary<T>
    {
        const int _setSize = 256;
        int _count = 0;

        readonly T[][][][] _store;


        public int Count => _count;

        public IntDictionary()
        {
            _store = new T[_setSize][][][];
        }

        class Cell
        {
            public bool IsValue => Array == null;
            Cell[] Array{get;set;}

            int Value{get;set;}
        }

        public void Add(int index, T value)
        {
            int index1 = index >> 24;
            int index2 = (index >> 16) & 0xFF;
            int index3 = (index >> 8) & 0xFF;
            int index4 = index & 0xFF;

            var first = _store[index1];
            if(first == null)
            {
                first = new T[_setSize][][];
                _store[index1] = first;
            }

            var second = first[index2];
            if(second == null)
            {
                second = new T[_setSize][];
                first[index2] = second;
            }

            var third = second[index3];
            if(third == null)
            {
                third = new T[_setSize];
                second[index3] = third;
            }

            third[index4] = value;
            _count++;
        }

        public T this[int index]
        {
            get 
            { 
                int index1 = index >> 24;
                int index2 = (index >> 16) & 0xFF;
                int index3 = (index >> 8) & 0xFF;
                int index4 = index & 0xFF;
                return _store[index1][index2][index3][index4];
            }
        }

        public void Remove(int index)
        {
            int index1 = index >> 24;
            int index2 = (index >> 16) & 0xFF;
            int index3 = (index >> 8) & 0xFF;
            int index4 = index & 0xFF;
            _store[index1][index2][index3][index4] = default(T);
            _count--;
        }

        public bool ContainsKey(int key)
        {
            throw new System.NotImplementedException();
        }
    }
}