namespace Ropu.Protocol;

public enum PacketTypes
{
    // Balancer Packets
    RegisterRouter = 0x00,
    RegisterRouterResponse = 0x01,
    RouterHeartbeat = 0x02,
    DistributorHeartbeat = 0x03,
    BalancerHeartbeatResponse = 0x04,
    RouterAssignmentRequest = 0x05,
    RouterAssignment = 0x06,
    RegisterDistributor = 0x07,
    RegisterDistributorResponse = 0x08,
    ResolveUnit = 0x09,
    ResolveUnitResponse = 0x0A,
    DistributorList = 0x0B,
    RequestDistributorList = 0x0C,

    // Router Packets
    RegisterClient = 0x0D,
    RegisterClientResponse = 0x0E,
    IndividualMessage = 0x0F,
    UnknownRecipient = 0x10,
    ClientHeartbeat = 0x11,
    ClientHeartbeatResponse = 0x12,
    GroupMessage = 0x13,
    SubscribeGroupsRequest = 0x14,
    SubscribeGroupsResponse = 0x15,
}