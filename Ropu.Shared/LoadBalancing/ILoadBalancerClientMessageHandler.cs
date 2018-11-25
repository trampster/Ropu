using System;
using System.Net;

namespace Ropu.Shared.LoadBalancing
{
    public interface ILoadBalancerClientMessageHandler
    {
        void HandleCallStart(uint requestId, ushort callId, ushort groupId);
        void HandleServingNodes(ushort requestId, Span<byte> nodeEndPointsData);
        void HandleServingNodeRemoved(ushort requestId, IPEndPoint endpoint);
        void HandleGroupCallManagers(ushort requestId, Span<byte> groupCallManagers);
        void HandleGroupCallManagerRemoved(ushort requestId, ushort groupId);
    }
}