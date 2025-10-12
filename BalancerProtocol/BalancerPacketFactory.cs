using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace Ropu.BalancerProtocol;

public enum DistributorChangeType
{
    Full = 0,
    Added = 1,
    Removed = 2
}

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

    public Span<byte> BuildRouterAssignmentRequestPacket(byte[] buffer, Guid clientId)
    {
        if (buffer.Length < 17)
        {
            throw new ArgumentException("Buffer is to small for router assignment request packet");
        }
        buffer[0] = (byte)BalancerPacketTypes.RouterAssignmentRequest;

        clientId.TryWriteBytes(buffer.AsSpan(1, 16));
        return buffer.AsSpan(0, 17);
    }

    public bool TryParseRouterAssignmentRequestPacket(Span<byte> buffer, out Guid clientId)
    {
        if (buffer.Length != 17)
        {
            clientId = Guid.Empty;
            return false;
        }
        clientId = new Guid(buffer.Slice(1, 16));

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
        Guid unitId)
    {
        buffer[0] = (byte)BalancerPacketTypes.ResolveUnit;


        unitId.TryWriteBytes(buffer.AsSpan(1, 16));
        return buffer.AsSpan(0, 17);
    }

    public bool TryParseResolveUnitPacket(
        Span<byte> buffer,
        out Guid unitId)
    {
        if (buffer.Length != 17)
        {
            unitId = Guid.Empty;
            return false;
        }

        unitId = new Guid(buffer.Slice(1, 16));
        return true;
    }

    public Span<byte> BuildResolveUnitResponse(
        byte[] buffer,
        bool success,
        Guid unitId,
        SocketAddress? routerAddress = null)
    {
        buffer[0] = (byte)BalancerPacketTypes.ResolveUnitResponse;

        buffer[1] = (byte)(success ? 0 : 1);

        unitId.TryWriteBytes(buffer.AsSpan(2, 16));

        var routerAddressSpan = buffer.AsSpan(18, 6);

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

        return buffer.AsSpan(0, 24);
    }


    public bool TryParseResolveUnitResponseResult(
        Span<byte> buffer,
        out bool success,
        out Guid unitId)
    {
        if (buffer.Length != 24)
        {
            unitId = Guid.Empty;
            success = false;
            return false;
        }

        success = buffer[0] == 0;

        unitId = new Guid(buffer.Slice(2, 16));
        return true;
    }

    public void ParseResolveUnitResponseSocketAddress(
        Span<byte> buffer,
        SocketAddress socketAddress)
    {
        socketAddress.ReadFromBytes(buffer.Slice(18, 6));
    }

    public Span<byte> BuildDistributorList(
        byte[] buffer,
        ushort sequenceNumber,
        DistributorChangeType changeType,
        Span<SocketAddress> distributors)
    {
        var span = buffer.AsSpan();
        buffer[0] = (byte)BalancerPacketTypes.DistributorList;
        BitConverter.TryWriteBytes(span.Slice(1, 2), sequenceNumber);
        buffer[3] = (byte)changeType;
        int socketStart = 4;
        foreach (var socketAddress in distributors)
        {
            socketAddress.WriteToBytes(span.Slice(socketStart, 6));
            socketStart += 6;
        }
        return buffer.AsSpan(0, 4 + (distributors.Length * 6));
    }

    public bool TryParseDistributorList(
        Span<byte> packet,
        SocketAddress[] addressesBuffer,
        out ushort sequenceNumber,
        out DistributorChangeType changeType,
        out Span<SocketAddress> distributors)
    {
        if (packet.Length < 3)
        {
            sequenceNumber = 0;
            changeType = DistributorChangeType.Added;
            distributors = Span<SocketAddress>.Empty;
            return false;
        }

        int payloadLength = packet.Length - 4;

        if (payloadLength % 6 != 0)
        {
            sequenceNumber = 0;
            changeType = DistributorChangeType.Added;
            distributors = Span<SocketAddress>.Empty;
            return false;
        }

        sequenceNumber = BitConverter.ToUInt16(packet.Slice(1, 2));
        changeType = (DistributorChangeType)packet[3];

        int outIndex = 0;
        for (int index = 4; index < packet.Length; index += 6)
        {
            addressesBuffer[outIndex].ReadFromBytes(packet.Slice(index, 6));
            outIndex++;
        }
        distributors = addressesBuffer.AsSpan(0, outIndex);
        return true;
    }

    public Span<byte> BuildRequestDistributorList(byte[] buffer)
    {
        buffer[0] = (byte)BalancerPacketTypes.RequestDistributorList;
        return buffer.AsSpan(0, 1);
    }
}