using System.Net;
using System.Net.Sockets;

namespace Ropu.BalancerProtocol;

/// <summary>
/// This is an unordered list of SocketAddresses
/// Properties: 
/// - Zero allocation once constructed
/// - Removing changes order
/// - Address are copied in when added allowing the original to be safely reused
/// </summary>
public class SocketAddressList
{
    readonly SocketAddress[] _addresses;
    int _count = 0;

    public SocketAddressList(int capacity)
    {
        _addresses = new SocketAddress[capacity];
        for (int index = 0; index < _addresses.Length; index++)
        {
            _addresses[index] = new SocketAddress(AddressFamily.InterNetwork);
        }
    }

    public void AddRange(Span<SocketAddress> rangeToAdd)
    {
        foreach (var address in rangeToAdd)
        {
            Add(address);
        }
    }

    public void Add(SocketAddress address)
    {
        _addresses[_count].CopyFrom(address);
        _count++;
    }

    public void Clear()
    {
        _count = 0;
    }

    public void RemoveRange(Span<SocketAddress> rangeToAdd)
    {
        foreach (var address in rangeToAdd)
        {
            Remove(address);
        }
    }

    public void Remove(SocketAddress addressToRemove)
    {
        for (int index = 0; index < _count; index++)
        {
            if (_addresses[index] == addressToRemove)
            {
                _addresses[index].CopyFrom(_addresses[_count - 1]);
                _count--;
            }
        }
    }

    public Span<SocketAddress> AsSpan()
    {
        return _addresses.AsSpan(0, _count);
    }
}