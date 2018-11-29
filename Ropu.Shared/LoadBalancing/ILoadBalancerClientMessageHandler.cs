using System;
using System.Net;

namespace Ropu.Shared.LoadBalancing
{
    public interface ILoadBalancerClientMessageHandler
    {
        void HandleCallStart(uint requestId, ushort callId, ushort groupId);
        void HandleServingNodes(ushort requestId, Span<byte> nodeEndPointsData);
        void HandleServingNodeRemoved(ushort requestId, IPEndPoint endpoint);
        void HandleGroupCallControllers(ushort requestId, Span<byte> groupCallControllers);
        void HandleGroupCallControllerRemoved(ushort requestId, ushort groupId);
        void HandleControllerRegistrationInfo(ushort requestId, byte controllerId, ushort refreshInterval, IPEndPoint endPoint);
    }
}