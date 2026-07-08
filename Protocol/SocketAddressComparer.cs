using System.Net;
using System.Net.Sockets;

namespace Ropu.Protocol;

public class SocketAddressComparer : IEqualityComparer<SocketAddress>
{
    public bool Equals(SocketAddress? x, SocketAddress? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.Family != y.Family) return false;

        // Uses optimized Span-based sequence equality
        return GetMeaningfulBytes(x).SequenceEqual(GetMeaningfulBytes(y));
    }

    public int GetHashCode(SocketAddress obj)
    {
        HashCode hash = new();
        hash.AddBytes(GetMeaningfulBytes(obj));
        return hash.ToHashCode();
    }

    static ReadOnlySpan<byte> GetMeaningfulBytes(SocketAddress socketAddress)
    {
        int length = socketAddress.Family switch
        {
            AddressFamily.InterNetwork => 16,
            AddressFamily.InterNetworkV6 => 28,
            _ => socketAddress.Size // Fallback for Unix sockets or other protocols
        };
        return socketAddress.Buffer.Span.Slice(0, length);
    }
}