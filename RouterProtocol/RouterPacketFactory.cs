namespace Ropu.RouterProtocol;

public class RouterPacketFactory
{
    public Span<byte> BuildRegisterClientPacket(byte[] buffer, Guid clientId)
    {
        buffer[0] = (byte)RouterPacketType.RegisterClient;
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
        buffer[0] = (byte)RouterPacketType.RegisterClientResponse;
        return buffer.AsSpan(0, 1);
    }

    public Span<byte> BuildIndividualMessagePacket(byte[] buffer, Guid clientId, Span<byte> payload)
    {
        buffer[0] = (byte)RouterPacketType.IndividualMessage;
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
        buffer[0] = (byte)RouterPacketType.IndividualMessage;
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
        buffer[0] = (byte)RouterPacketType.UnknownRecipient;
        unitId.TryWriteBytes(buffer.AsSpan(1, 16));
        return buffer.AsSpan(0, 17);
    }

    public static byte[] HeartbeatPacket = [(byte)RouterPacketType.Heartbeat];

    public static byte[] HeartbeatResponsePacket = [(byte)RouterPacketType.HeartbeatResponse];
}