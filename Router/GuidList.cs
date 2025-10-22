using System.Net;
using System.Net.Sockets;

namespace Ropu.Router;

/// <summary>
/// This is an unordered list of SocketAddresses
/// Properties: 
/// - Zero allocation once constructed
/// - Removing changes order
/// - Address are copied in when added allowing the original to be safely reused
/// </summary>
public class GuidList
{
    Guid[] _guids;
    int _count = 0;

    public GuidList(int initialCapacity)
    {
        _guids = new Guid[initialCapacity];
    }

    public void AddRange(Span<Guid> rangeToAdd)
    {
        foreach (var guid in rangeToAdd)
        {
            Add(guid);
        }
    }

    public void Add(Guid guid)
    {
        if (_count == _guids.Length)
        {
            Array.Resize(ref _guids, _guids.Length * 2);
        }
        _guids[_count] = guid;
        _count++;
    }

    public void Clear()
    {
        _count = 0;
    }

    public void RemoveRange(Span<Guid> rangeToAdd)
    {
        foreach (var guid in rangeToAdd)
        {
            Remove(guid);
        }
    }

    public void Remove(Guid guid)
    {
        for (int index = 0; index < _count; index++)
        {
            if (_guids[index] == guid)
            {
                _guids[index] = _guids[_count - 1];
                _count--;
            }
        }
    }

    public Span<Guid> AsSpan()
    {
        return _guids.AsSpan(0, _count);
    }
}