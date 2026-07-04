using System.Net;
using System.Net.Sockets;

namespace Ropu.Protocol;

public class RopuSocket : IDisposable
{
    readonly Socket _socket;
    readonly BulkSender _bulkSender;

    public RopuSocket(ushort port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var endpoint = new IPEndPoint(IPAddress.Any, port);
        _socket.Bind(endpoint);
        _bulkSender = new BulkSender(_socket);
    }

    public void SendBulk(ReadOnlyMemory<byte> buffer, Memory<SocketAddress> destinations, SocketAddress? except)
    {
        _bulkSender.SendBulk(buffer, destinations, except);
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