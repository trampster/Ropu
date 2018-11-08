using System.Net;

namespace Ropu.Shared.LoadBalancing
{
    public interface ILoadBalancerServerMessageHandler
    {
        void HandleRegisterServingNode(IPEndPoint from, ushort requestId, IPEndPoint mediaEndpoint);
        void HandleRegisterCallController(IPEndPoint from, ushort requestId, IPEndPoint callControlEndpoint);
        void HandleRequestServingNode(ushort requestId, IPEndPoint endPoint);
    }
}