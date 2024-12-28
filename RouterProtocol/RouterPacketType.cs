namespace Ropu.RouterProtocol;

public enum RouterPacketType
{
    RegisterClient = 0x00,
    RegisterClientResponse = 0x01,
    SendToClient = 0x02,
    UnknownRecipient = 0x03,
    Heartbeat = 0x04,
    HeartbeatResponse = 0x05
}
