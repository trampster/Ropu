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

    public Span<byte> BuildIndividualMessagePacket(byte[] buffer, Guid clientId, Span<byte> payload)
    {
        buffer[0] = (byte)PacketTypes.IndividualMessage;
        clientId.TryWriteBytes(buffer.AsSpan(1, 16));

        payload.CopyTo(buffer.AsSpan(17));

        return buffer.AsSpan(0, 17 + payload.Length);
    }

    public bool TryParseIndividualMessagePacket(Span<byte> packet, out Guid clientId, out Span<byte> payload)
    {
        if (packet.Length < 17)
        {
            clientId = Guid.Empty;
            payload = [];
            return false;
        }

        clientId = new Guid(packet.Slice(1, 16));
        payload = packet.Slice(17);
        return true;
    }

    public Span<byte> BuildGroupMessagePacket(byte[] buffer, Guid groupId, Span<byte> payload)
    {
        buffer[0] = (byte)PacketTypes.IndividualMessage;
        groupId.TryWriteBytes(buffer.AsSpan(1, 16));

        payload.CopyTo(buffer.AsSpan(17));

        return buffer.AsSpan(0, 17 + payload.Length);
    }

    public bool TryParseGroupMessagePacket(Span<byte> packet, out Guid groupId, out Span<byte> payload)
    {
        if (packet.Length < 17)
        {
            groupId = Guid.Empty;
            payload = [];
            return false;
        }

        groupId = new Guid(packet.Slice(1, 16));
        payload = packet.Slice(17);
        return true;
    }

    public bool TryParseUnitIdFromIndividualMessagePacket(Span<byte> packet, out Guid clientId)
    {
        if (packet.Length < 17)
        {
            clientId = Guid.Empty;
            return false;
        }

        clientId = new Guid(packet.Slice(1, 16));
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

    public bool TryParseSubscribeGroupsRequest(Span<byte> packet, Guid[] groupsBuffer, out Span<Guid> groups)
    {
        var payloadLength = groupsBuffer.Length - 1;
        if (payloadLength % 16 != 0)
        {
            groups = Span<Guid>.Empty;
            return false;
        }

        int outIndex = 0;
        for (int bufferIndex = 0; bufferIndex < packet.Length; bufferIndex += 16)
        {
            groupsBuffer[outIndex] = new Guid(packet.Slice(1, 16));
            outIndex++;
        }
        groups = groupsBuffer.AsSpan(0, outIndex);
        return true;
    }

    public static byte[] HeartbeatPacket = [(byte)PacketTypes.ClientHeartbeat];

    public static byte[] HeartbeatResponsePacket = [(byte)PacketTypes.ClientHeartbeatResponse];
}