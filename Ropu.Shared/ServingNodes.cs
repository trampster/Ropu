using System;
using System.Collections.Generic;
using System.Net;
using Ropu.Shared.Concurrent;

namespace Ropu.Shared
{

    public class ServingNodes
    {
        readonly SnapshotSet<IPEndPoint> _set;

        public ServingNodes(int max)
        {
            _set = new SnapshotSet<IPEndPoint>(max);
        }

        public void HandleServingNodesPayload(Span<byte> nodeEndPointsData)
        {
            for(int index = 0; index < nodeEndPointsData.Length; index +=6)
            {
                var endPoint = nodeEndPointsData.Slice(index).ParseIPEndPoint();
                Console.WriteLine($"Added Serving Node EndPoint {endPoint}");
                _set.Add(endPoint);
            }
        }

        public SnapshotSet<IPEndPoint> EndPoints
        {
            get
            {
                return _set;
            }
        }

        public void RemoveServingNode(IPEndPoint endPoint)
        {
            _set.Remove(endPoint);
        }
    }
}