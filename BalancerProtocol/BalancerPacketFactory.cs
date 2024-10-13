using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

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

    public Span<byte> BuildRouterAssignmentPacket(byte[] buffer, SocketAddress endPoint)
    {
        if (buffer.Length < 7)
        {
            throw new ArgumentException("Buffer is to small for router assignment packet");
        }
        buffer[0] = (byte)BalancerPacketTypes.RouterAssignment;

        endPoint.WriteToBytes(buffer.AsSpan(1, 6));
        return buffer.AsSpan(0, 7);
    }

    public bool TryParseRouterAssignmentPacket(Span<byte> buffer, SocketAddress socketAddress)
    {
        if (buffer.Length != 7)
        {
            return false;
        }
        socketAddress.ReadFromBytes(buffer.Slice(1, 6));
        return true;
    }

    public Span<byte> BuildRegisterRouterPacket(
        byte[] buffer,
        SocketAddress routerAddress,
        ushort capacity)
    {
        buffer[0] = (byte)BalancerPacketTypes.RegisterRouter;

        routerAddress.WriteToBytes(buffer.AsSpan(1, 6));
        BitConverter.TryWriteBytes(buffer.AsSpan(7, 2), capacity);
        return buffer.AsSpan(0, 11);
    }

    public bool TryParseRouterRegisterPacket(
        Span<byte> buffer,
        SocketAddress address,
        [NotNullWhen(true)] out ushort? capacity)
    {
        if (buffer.Length != 11)
        {
            capacity = 0;
            return false;
        }

        address.ReadFromBytes(buffer.Slice(1, 6));
        capacity = BitConverter.ToUInt16(buffer.Slice(7, 2));
        return true;
    }

    public Span<byte> BuildRouterInfoPageRequest(
        byte[] buffer,
        byte pageNumber)
    {
        if (buffer.Length < 2)
        {
            throw new ArgumentException("Buffer is to small for Router Info Page Request packet");
        }
        buffer[0] = (byte)BalancerPacketTypes.RouterInfoPageRequest;
        buffer[1] = pageNumber;
        return buffer.AsSpan(0, 2);
    }

    public bool TryParseRouterInfoPageRequest(
        Span<byte> buffer,
        [NotNullWhen(true)] out byte pageNumber)
    {
        if (buffer.Length != 2)
        {
            pageNumber = 0;
            return false;
        }

        pageNumber = buffer[1];
        return true;
    }

    public int BuildRouterInfoPageHeader(
        Span<byte> buffer,
        byte pageNumber)
    {
        if (buffer.Length < 2)
        {
            throw new ArgumentException("Buffer is to small for Router Info Page Header");
        }
        buffer[0] = (byte)BalancerPacketTypes.RouterInfoPage;
        buffer[1] = pageNumber;
        return 2;
    }

    public int WriteRouterInfoPageEntry(Span<byte> buffer, ushort routerId, SocketAddress routerAddress)
    {
        BitConverter.TryWriteBytes(buffer.Slice(0, 2), routerId);
        routerAddress.WriteToBytes(buffer.Slice(2, 6));
        return 8;
    }

    public bool TryParseRouterInfoPage(Span<byte> buffer, out byte pageNumber, out Span<byte> routersSection)
    {
        if (buffer.Length < 2)
        {
            pageNumber = 0;
            routersSection = buffer.Slice(0, 0);
            return false;
        }

        pageNumber = buffer[1];
        routersSection = buffer.Slice(2);
        return true;
    }

    public bool TryParseRouterInfo(Span<byte> buffer, out ushort routerId, SocketAddress address)
    {
        if (buffer.Length < 8)
        {
            routerId = 0;
            return false;
        }
        routerId = BitConverter.ToUInt16(buffer.Slice(0, 2));
        address.ReadFromBytes(buffer.Slice(2, 6));
        return true;
    }
}