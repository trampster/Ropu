using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Ropu.BalancerProtocol;

public class BalancerPacketFactory
{
    public Span<byte> BuildRegisterRouterResponsePacket(byte[] buffer, ushort routerId)
    {
        buffer[0] = (byte)BalancerPacketTypes.RegisterRouterResponse;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 2), routerId);
        return buffer.AsSpan(0, 3);
    }

    public bool TryParseRegisterRouterResponsePacket(Span<byte> packet, out ushort routerId)
    {
        if (packet.Length != 3)
        {
            routerId = 0;
            return false;
        }

        routerId = BitConverter.ToUInt16(packet.Slice(1, 2));
        return true;
    }

    readonly byte[] _heartbeatResponse = [(byte)BalancerPacketTypes.HeartbeatResponse];
    public byte[] HeartbeatResponse => _heartbeatResponse;

    public Span<byte> BuildHeartbeatPacket(
        byte[] buffer,
        ushort routerId,
        ushort registeredUsers)
    {
        buffer[0] = (byte)BalancerPacketTypes.Heartbeat;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 2), routerId);
        BitConverter.TryWriteBytes(buffer.AsSpan(3, 2), registeredUsers);
        return buffer.AsSpan(0, 5);
    }

    public bool TryParseHeartbeatPacket(Span<byte> buffer,
        [NotNullWhen(true)] out HeartbeatPacket? heartbeatPacket)
    {
        if (buffer.Length != 5)
        {
            heartbeatPacket = null;
            return false;
        }
        heartbeatPacket = new()
        {
            RouterId = BitConverter.ToUInt16(buffer.Slice(1, 2)),
            RegisteredUsers = BitConverter.ToUInt16(buffer.Slice(3, 2))
        };
        return true;
    }

    readonly byte[] _routerAssignmentRequest = [(byte)BalancerPacketTypes.RouterAssignmentRequest];
    public byte[] RouterAssignmentRequest => _routerAssignmentRequest;

    public Span<byte> BuildRouterAssignmentPacket(byte[] buffer, IPEndPoint endPoint)
    {
        if (buffer.Length < 7)
        {
            throw new ArgumentException("Buffer is to small for router assignment packet");
        }
        buffer[0] = (byte)BalancerPacketTypes.RouterAssignment;

        WriteEndpoint(buffer.AsSpan(1), endPoint);
        return buffer.AsSpan(0, 7);
    }

    void WriteEndpoint(Span<byte> buffer, IPEndPoint endPoint)
    {
        endPoint.Address.TryWriteBytes(buffer.Slice(0, 4), out int bytesWritten);
        BitConverter.TryWriteBytes(buffer.Slice(4, 2), (ushort)endPoint.Port);
    }

    public bool TryParseRouterAssignmentPacket(Span<byte> buffer, [NotNullWhen(true)] out IPEndPoint? endpoint)
    {
        if (buffer.Length != 7)
        {
            endpoint = null;
            return false;
        }
        var address = new IPAddress(buffer.Slice(1, 4));
        var port = BitConverter.ToUInt16(buffer.Slice(5, 2));
        endpoint = new IPEndPoint(address, port);
        return true;
    }

    public Span<byte> BuildRegisterRouterPacket(
        byte[] buffer,
        IPEndPoint routerIPEndpoint,
        ushort capacity)
    {
        buffer[0] = (byte)BalancerPacketTypes.RegisterRouter;
        WriteEndpoint(buffer.AsSpan(1), routerIPEndpoint);
        BitConverter.TryWriteBytes(buffer.AsSpan(7, 2), capacity);
        return buffer.AsSpan(0, 11);
    }

    public bool TryParseRouterRegisterPacket(
        Span<byte> buffer,
        [NotNullWhen(true)] out RouterRegisterPacket? packet)
    {
        if (buffer.Length != 11)
        {
            packet = null;
            return false;
        }

        var address = new IPAddress(buffer.Slice(1, 4));
        var port = BitConverter.ToUInt16(buffer.Slice(5, 2));

        ushort capacity = BitConverter.ToUInt16(buffer.Slice(7, 2));

        packet = new RouterRegisterPacket()
        {
            EndPoint = new IPEndPoint(address, port),
            Capacity = capacity
        };

        return true;
    }
}