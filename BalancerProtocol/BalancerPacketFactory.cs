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

    public Span<byte> BuildRegisterDistributorResponsePacket(byte[] buffer, ushort distributorId)
    {
        buffer[0] = (byte)BalancerPacketTypes.RegisterDistributorResponse;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 2), distributorId);
        return buffer.AsSpan(0, 3);
    }

    public bool TryParseRegisterDsitributorResponsePacket(Span<byte> packet, out ushort distributorId)
    {
        if (packet.Length != 3)
        {
            distributorId = 0;
            return false;
        }

        distributorId = BitConverter.ToUInt16(packet.Slice(1, 2));
        return true;
    }

    readonly byte[] _heartbeatResponse = [(byte)BalancerPacketTypes.HeartbeatResponse];
    public byte[] HeartbeatResponse => _heartbeatResponse;

    public Span<byte> BuildHeartbeatPacket(
        byte[] buffer,
        BalancerPacketTypes heartbeatType,
        ushort id,
        ushort registeredUsers)
    {
        buffer[0] = (byte)heartbeatType;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 2), id);
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
            Id = BitConverter.ToUInt16(buffer.Slice(1, 2)),
            NumberRegistered = BitConverter.ToUInt16(buffer.Slice(3, 2))
        };
        return true;
    }

    public Span<byte> BuildRouterAssignmentRequestPacket(byte[] buffer, uint clientId)
    {
        if (buffer.Length < 5)
        {
            throw new ArgumentException("Buffer is to small for router assignment request packet");
        }
        buffer[0] = (byte)BalancerPacketTypes.RouterAssignmentRequest;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 4), clientId);
        return buffer.AsSpan(0, 5);
    }

    public bool TryParseRouterAssignmentRequestPacket(Span<byte> buffer, out int clientId)
    {
        if (buffer.Length != 5)
        {
            clientId = 0;
            return false;
        }
        clientId = BitConverter.ToInt32(buffer.Slice(1, 4));

        return true;
    }

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

    public Span<byte> BuildRegisterDistributorPacket(
        byte[] buffer,
        SocketAddress distributorAddress,
        ushort capacity)
    {
        buffer[0] = (byte)BalancerPacketTypes.RegisterDistributor;

        distributorAddress.WriteToBytes(buffer.AsSpan(1, 6));
        BitConverter.TryWriteBytes(buffer.AsSpan(7, 2), capacity);
        return buffer.AsSpan(0, 11);
    }

    public bool TryParseRegisterDistributorPacket(
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

    public Span<byte> BuildResolveUnit(
        byte[] buffer,
        int unitId)
    {
        buffer[0] = (byte)BalancerPacketTypes.ResolveUnit;

        BitConverter.TryWriteBytes(buffer.AsSpan(1, 4), unitId);
        return buffer.AsSpan(0, 5);
    }

    public bool TryParseResolveUnitPacket(
        Span<byte> buffer,
        out int unitId)
    {
        if (buffer.Length != 5)
        {
            unitId = 0;
            return false;
        }

        unitId = BitConverter.ToInt32(buffer.Slice(1, 4));
        return true;
    }

    public Span<byte> BuildResolveUnitResponse(
        byte[] buffer,
        bool success,
        int unitId,
        SocketAddress? routerAddress = null)
    {
        buffer[0] = (byte)BalancerPacketTypes.ResolveUnit;

        buffer[1] = (byte)(success ? 0 : 1);

        BitConverter.TryWriteBytes(buffer.AsSpan(2, 4), unitId);

        var routerAddressSpan = buffer.AsSpan(6, 6);

        if (routerAddress == null)
        {
            for (int index = 0; index < routerAddressSpan.Length; index++)
            {
                buffer[index] = 0;
            }
        }
        else
        {
            routerAddress.WriteToBytes(routerAddressSpan);
        }

        return buffer.AsSpan(0, 12);
    }


    public bool TryParseResolveUnitResponse(
        Span<byte> buffer,
        out bool success,
        out int unitId,
        SocketAddress socketAddress)
    {
        if (buffer.Length != 12)
        {
            unitId = 0;
            success = false;
            return false;
        }

        success = buffer[0] == 0;

        unitId = BitConverter.ToInt32(buffer.Slice(2, 4));

        socketAddress.ReadFromBytes(buffer.Slice(6, 6));

        return true;
    }
}