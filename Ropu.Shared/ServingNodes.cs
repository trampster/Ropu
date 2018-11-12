using System;
using System.Collections.Generic;
using System.Net;
using Ropu.Shared.Concurrent;

namespace Ropu.Shared
{

    public class ServingNodes
    {
        readonly Set<IPEndPoint> _set;

        public ServingNodes(int max)
        {
            _set = new Set<IPEndPoint>(max);
        }

        public void HandleServingNodesPayload(Span<byte> nodeEndPointsData)
        {
            _set.SuspendRefresh();
            for(int index = 0; index < nodeEndPointsData.Length; index +=6)
            {
                var endPoint = nodeEndPointsData.Slice(index).ParseIPEndPoint();
                _set.Add(endPoint);
            }
            _set.ResumeRefresh();
        }

        public ReusableMemory<IPEndPoint> EndPoints
        {
            get
            {
                return _set.Get();
            }
        }

        public void RemoveServingNode(IPEndPoint endPoint)
        {
            _set.Remove(endPoint);
        }

        public void Recycle(ReusableMemory<IPEndPoint> used)
        {
            _set.Recycle(used);
        }
    }
}