using System.Net;

namespace Ropu.BalancerProtocol;

public static class SocketAddressExtensions
{
    public static void SetPort(this SocketAddress socketAddress, ushort port)
    {
        socketAddress[2] = (byte)(port >> 8);
        socketAddress[3] = (byte)(port & 0xFF);
    }

    public static ushort GetPort(this SocketAddress socketAddress)
    {
        return (ushort)((socketAddress[2] << 8) + socketAddress[3]);
    }

    public static void SetAddress(this SocketAddress socketAddress, IPAddress ipAddress)
    {
        if (!ipAddress.TryWriteBytes(socketAddress.Buffer.Span.Slice(4, 4), out int bytesWritten))
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// This allocates memory and should only be used for debugging
    /// </summary>
    public static IPAddress GetAddress(this SocketAddress socketAddress)
    {
        return new IPAddress(socketAddress.Buffer.Span.Slice(4, 4));
    }

    /// <summary>
    /// Assumes IPv4
    /// </summary>
    public static bool WriteToBytes(this SocketAddress socketAddress, Span<byte> buffer)
    {
        if (buffer.Length != 6)
        {
            return false;
        }
        socketAddress.Buffer.Span.Slice(2, 6).CopyTo(buffer);
        return true;
    }

    /// <summary>
    /// Assumes IPv4
    /// </summary>
    public static bool ReadFromBytes(this SocketAddress socketAddress, Span<byte> buffer)
    {
        if (buffer.Length != 6)
        {
            return false;
        }
        buffer.CopyTo(socketAddress.Buffer.Span.Slice(2, 6));
        return true;
    }

    public static void CopyFrom(this SocketAddress socketAddress, SocketAddress otherAddress)
    {
        otherAddress.Buffer.Span.Slice(2, 6).CopyTo(socketAddress.Buffer.Span.Slice(2, 6));
    }

    /// <summary>
    /// Allocates memory should only be used for debuggin
    /// </summary>
    public static string DebugString(this SocketAddress socketAddress)
    {
        return $"{socketAddress.GetAddress()}:{socketAddress.GetPort()}";
    }
}