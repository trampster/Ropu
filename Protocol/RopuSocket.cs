using System.Net;
using System.Net.Sockets;

namespace Ropu.Protocol;

public class RopuSocket : IDisposable
{
    readonly Socket _socket;

    public RopuSocket(ushort port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var endpoint = new IPEndPoint(IPAddress.Any, port);
        _socket.Bind(endpoint);
    }

    public void SendTo(Span<byte> packet, SocketAddress address)
    {
        _socket.SendTo(packet, SocketFlags.None, address);
    }

    public int ReceiveFrom(SocketAddress socketAddress, byte[] receiveBuffer)
    {
        return _socket.ReceiveFrom(receiveBuffer, SocketFlags.None, socketAddress);
    }

    public void Close() => _socket.Close();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _socket.Dispose();
        }

    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}