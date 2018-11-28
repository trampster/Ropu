namespace Ropu.Shared.LoadBalancing
{
    public enum LoadBalancerPacketType
    {
        RegisterServingNode = 0,
        RegisterCallController = 1,
        StartCall = 2,
        Ack = 3,
        RequestServingNode = 4,
        ServingNodeResponse = 5,
        ServingNodes = 6,
        ServingNodeRemoved = 7,
        GroupCallControllers = 8,
        GroupCallControllerRemoved = 9,
    }
}