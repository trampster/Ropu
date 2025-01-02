namespace Ropu.RouterProtocol;

public class RouterPacketFactory
{
    public Span<byte> BuildRegisterClientPacket(byte[] buffer, uint clientId)
    {
        buffer[0] = (byte)RouterPacketType.RegisterClient;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 4), clientId);
        return buffer.AsSpan(0, 5);
    }

    public bool TryParseRegisterClientPacket(Span<byte> packet, out uint clientId)
    {
        if (packet.Length != 5)
        {
            clientId = 0;
            return false;
        }

        clientId = BitConverter.ToUInt32(packet.Slice(1, 4));
        return true;
    }

    public Span<byte> BuildRegisterClientResponse(byte[] buffer)
    {
        buffer[0] = (byte)RouterPacketType.RegisterClientResponse;
        return buffer.AsSpan(0, 1);
    }

    public Span<byte> BuildIndividualMessagePacket(byte[] buffer, uint clientId, Span<byte> payload)
    {
        buffer[0] = (byte)RouterPacketType.IndividualMessage;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 4), clientId);

        payload.CopyTo(buffer.AsSpan(5));

        return buffer.AsSpan(0, 5 + payload.Length);
    }

    public bool TryParseIndividualMessagePacket(Span<byte> packet, out uint clientId, out Span<byte> payload)
    {
        if (packet.Length < 5)
        {
            clientId = 0;
            payload = [];
            return false;
        }

        clientId = BitConverter.ToUInt32(packet.Slice(1, 4));
        payload = packet.Slice(5);
        return true;
    }

    public bool TryParseUnitIdFromIndividualMessagePacket(Span<byte> packet, out uint clientId)
    {
        if (packet.Length < 5)
        {
            clientId = 0;
            return false;
        }

        clientId = BitConverter.ToUInt32(packet.Slice(1, 4));
        return true;
    }

    public bool TryParseUnknownRecipientPacket(Span<byte> packet, out uint unitId)
    {
        if (packet.Length != 5)
        {
            unitId = 0;
            return false;
        }

        unitId = BitConverter.ToUInt32(packet.Slice(1, 4));
        return true;
    }

    public Span<byte> BuildUnknownRecipientPacket(byte[] buffer, uint unitId)
    {
        buffer[0] = (byte)RouterPacketType.UnknownRecipient;
        BitConverter.TryWriteBytes(buffer.AsSpan(1, 4), unitId);
        return buffer.AsSpan(0, 5);
    }

    public static byte[] HeartbeatPacket = [(byte)RouterPacketType.Heartbeat];

    public static byte[] HeartbeatResponsePacket = [(byte)RouterPacketType.HeartbeatResponse];
}