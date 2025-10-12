namespace Ropu.BalancerProtocol;

public enum BalancerPacketTypes
{
    RegisterRouter = 0x00,
    RegisterRouterResponse = 0x01,
    RouterHeartbeat = 0x02,
    DistributorHeartbeat = 0x03,
    HeartbeatResponse = 0x04,
    RouterAssignmentRequest = 0x05,
    RouterAssignment = 0x06,
    RegisterDistributor = 0x07,
    RegisterDistributorResponse = 0x08,
    ResolveUnit = 0x09,
    ResolveUnitResponse = 0x0A,
    DistributorList = 0x0B,
    RequestDistributorList = 0x0C,
}