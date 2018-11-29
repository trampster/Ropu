namespace Ropu.Shared.LoadBalancing
{
    public enum LoadBalancerPacketType
    {
        RegisterServingNode = 0,
        RegisterCallController = 1,
        ControllerRegistrationInfo = 2,
        RefreshCallController = 3,
        StartCall = 4,
        Ack = 5,
        RequestServingNode = 6,
        ServingNodeResponse = 7,
        ServingNodes = 8,
        ServingNodeRemoved = 9,
        GroupCallControllers = 10,
        GroupCallControllerRemoved = 11,
    }
}