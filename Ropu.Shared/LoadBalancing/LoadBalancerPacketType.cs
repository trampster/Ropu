namespace Ropu.Shared.LoadBalancing
{
    public enum LoadBalancerPacketType
    {
        RegisterServingNode = 0,
        RegisterCallController = 1,
        StartCall = 2,
        Ack = 3,
        RegistrationUpdate = 4,
        RegistrationRemoved = 5,
        RequestServingNode = 6,
        ServingNodeResponse = 7
    }
}