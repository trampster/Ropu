namespace Ropu.Protocol;

public class RouterPacketFactory
{
    public Span<byte> BuildRegisterClientPacket(byte[] buffer, Guid clientId)
    {
        buffer[0] = (byte)PacketTypes.RegisterClient;
        clientId.TryWriteBytes(buffer.AsSpan(1, 16));
        return buffer.AsSpan(0, 17);
    }

    public bool TryParseRegisterClientPacket(Span<byte> packet, out Guid clientId)
    {
        if (packet.Length != 17)
        {
            clientId = Guid.Empty;
            return false;
        }

        clientId = new Guid(packet.Slice(1, 16));
        return true;
    }

    public Span<byte> BuildRegisterClientResponse(byte[] buffer)
    {
        buffer[0] = (byte)PacketTypes.RegisterClientResponse;
        return buffer.AsSpan(0, 1);
    }

    public Span<byte> BuildIndividualMessagePacket(byte[] buffer, Guid fromClientId, Guid toClientId, Span<byte> payload)
    {
        buffer[0] = (byte)PacketTypes.IndividualMessage;
        fromClientId.TryWriteBytes(buffer.AsSpan(1, 16));
        toClientId.TryWriteBytes(buffer.AsSpan(17, 16));

        payload.CopyTo(buffer.AsSpan(33));

        return buffer.AsSpan(0, 33 + payload.Length);
    }

    public bool TryParseIndividualMessagePacket(Span<byte> packet, out Guid fromClientId, out Guid toClientId, out Span<byte> payload)
    {
        if (packet.Length < 34)
        {
            fromClientId = Guid.Empty;
            toClientId = Guid.Empty;
            payload = [];
            return false;
        }

        fromClientId = new Guid(packet.Slice(1, 16));
        toClientId = new Guid(packet.Slice(17, 16));
        payload = packet.Slice(33);
        return true;
    }

    public Span<byte> BuildGroupMessagePacket(
        byte[] buffer,
        Guid fromClientId,
        Guid groupId,
        GroupMessageType groupMessageType,
        Span<byte> payload)
    {
        buffer[0] = (byte)PacketTypes.GroupMessage;
        fromClientId.TryWriteBytes(buffer.AsSpan(1, 16));
        groupId.TryWriteBytes(buffer.AsSpan(17, 16));

        buffer[33] = (byte)groupMessageType;

        payload.CopyTo(buffer.AsSpan(34));

        return buffer.AsSpan(0, 34 + payload.Length);
    }

    public bool TryParseGroupMessagePacket(
        Span<byte> packet,
        out Guid fromClientId,
        out Guid groupId,
        out GroupMessageType groupMessageType,
        out Span<byte> payload)
    {
        if (packet.Length < 18)
        {
            fromClientId = Guid.Empty;
            groupId = Guid.Empty;
            payload = [];
            groupMessageType = GroupMessageType.OneOff;
            return false;
        }

        fromClientId = new Guid(packet.Slice(1, 16));
        groupId = new Guid(packet.Slice(17, 16));
        groupMessageType = (GroupMessageType)packet[33];
        payload = packet.Slice(34);
        return true;
    }

    public bool TryParseToUnitIdFromIndividualMessagePacket(Span<byte> packet, out Guid clientId)
    {
        if (packet.Length < 33)
        {
            clientId = Guid.Empty;
            return false;
        }

        clientId = new Guid(packet.Slice(17, 16));
        return true;
    }

    public bool TryParseUnknownRecipientPacket(Span<byte> packet, out Guid unitId)
    {
        if (packet.Length != 17)
        {
            unitId = Guid.Empty;
            return false;
        }

        unitId = new Guid(packet.Slice(1, 16));
        return true;
    }

    public Span<byte> BuildUnknownRecipientPacket(byte[] buffer, Guid unitId)
    {
        buffer[0] = (byte)PacketTypes.UnknownRecipient;
        unitId.TryWriteBytes(buffer.AsSpan(1, 16));
        return buffer.AsSpan(0, 17);
    }

    public Span<byte> BuildSubscribeGroupsRequest(byte[] buffer, Span<Guid> groups)
    {
        buffer[0] = (byte)PacketTypes.SubscribeGroupsRequest;

        int bufferIndex = 1;
        for (int index = 0; index < groups.Length; index++)
        {
            var groupGuid = groups[index];
            groupGuid.TryWriteBytes(buffer.AsSpan(bufferIndex, 16));
            bufferIndex += 16;
        }
        return buffer.AsSpan(0, bufferIndex);
    }

    public Span<byte> BuildSubscribeGroupsResponse(byte[] buffer)
    {
        buffer[0] = (byte)PacketTypes.SubscribeGroupsResponse;
        return buffer.AsSpan(0, 1);
    }

    public bool TryParseSubscribeGroupsRequest(Span<byte> packet, Guid[] groupsBuffer, out Span<Guid> groups)
    {
        var payloadLength = packet.Length - 1;
        if (payloadLength < 0 || payloadLength % 16 != 0)
        {
            groups = Span<Guid>.Empty;
            return false;
        }

        var expectedGroupsCount = payloadLength / 16;
        if (groupsBuffer.Length < expectedGroupsCount)
        {
            groups = Span<Guid>.Empty;
            return false;
        }

        int outIndex = 0;
        for (int bufferIndex = 1; bufferIndex < packet.Length; bufferIndex += 16)
        {
            groupsBuffer[outIndex] = new Guid(packet.Slice(bufferIndex, 16));
            outIndex++;
        }
        groups = groupsBuffer.AsSpan(0, outIndex);
        return true;
    }

    public static byte[] HeartbeatPacket = [(byte)PacketTypes.ClientHeartbeat];

    public static byte[] HeartbeatResponsePacket = [(byte)PacketTypes.ClientHeartbeatResponse];

    public Memory<byte> BuildDistributorCapacityPacket(byte[] buffer, ushort capacity)
    {
        buffer[0] = (byte)PacketTypes.DistributorCapacity;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 2), capacity);
        return buffer.AsMemory(0, 3);
    }

    public bool TryParseDistributorCapacityPacket(Span<byte> packet, out ushort capacity)
    {
        if (packet.Length != 3)
        {
            capacity = 0;
            return false;
        }

        capacity = BitConverter.ToUInt16(packet.Slice(1, 2));
        return true;
    }

    public Span<byte> BuildGroupMessageFailureResponse(byte[] buffer, Guid groupId, GroupPacketFailureReason failureReason)
    {
        buffer[0] = (byte)PacketTypes.GroupMessageFailureResponse;
        groupId.TryWriteBytes(buffer.AsSpan(1, 16));
        buffer[17] = (byte)failureReason;
        return buffer.AsSpan(0, 18);
    }

    public bool TryParseGroupMessageFailureResponse(
        Span<byte> packet,
        out Guid unitId,
        out GroupPacketFailureReason groupPacketFailureReason)
    {
        if (packet.Length != 18)
        {
            unitId = Guid.Empty;
            groupPacketFailureReason = GroupPacketFailureReason.Unknown;
            return false;
        }

        unitId = new Guid(packet.Slice(1, 16));
        groupPacketFailureReason = (GroupPacketFailureReason)packet[17];
        return true;
    }

}