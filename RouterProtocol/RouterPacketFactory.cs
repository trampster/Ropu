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

    public static byte[] HeartbeatPacket = [(byte)RouterPacketType.Heartbeat];

    public static byte[] HeartbeatResponsePacket = [(byte)RouterPacketType.HeartbeatResponse];
}