namespace Ropu.BalancerProtocol;

public enum BalancerPacketTypes
{
    RegisterRouter = 0x00,
    RegisterRouterResponse = 0x01,
    Heartbeat = 0x02,
    HeartbeatResponse = 0x03,
    RouterAssignmentRequest = 0x04,
    RouterAssignment = 0x05
}