using System.Net;
using System.Net.Sockets;

namespace Ropu.Protocol;

/// <summary>
/// This is an unordered list of T
/// Properties: 
/// - Zero allocation once constructed
/// - Removing changes order
/// </summary>
public class UnorderedList<T> where T : new()
{
    T[] _items;
    int _count = 0;

    public UnorderedList(int capacity)
    {
        _items = new T[capacity];
        for (int index = 0; index < _items.Length; index++)
        {
            _items[index] = new T();
        }
    }

    public int Length => _count;

    public T Add()
    {
        if (_count == _items.Length)
        {
            Array.Resize(ref _items, _count * 2);
            _items[_count] = new T();
        }
        var item = _items[_count];
        _count++;
        return item;
    }

    public void Clear()
    {
        _count = 0;
    }

    public void Remove(T itemToRemove)
    {
        for (int index = 0; index < _count; index++)
        {
            if (_items[index]!.Equals(itemToRemove))
            {
                _items[index] = _items[_count - 1];
                _items[_count - 1] = itemToRemove;
                _count--;
            }
        }
    }

    public void RemoveAt(int index)
    {
        var itemToRemove = _items[index];
        _items[index] = _items[_count - 1];
        _items[_count - 1] = itemToRemove;
        _count--;
    }

    public Span<T> AsSpan()
    {
        return _items.AsSpan(0, _count);
    }

    public Memory<T> AsMemory()
    {
        return _items.AsMemory(0, _count);
    }
}