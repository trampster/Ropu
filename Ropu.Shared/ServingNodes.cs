using System;
using System.Collections.Generic;
using System.Net;

namespace Ropu.Shared
{
    public class ServingNodes
    {
        public List<IPEndPoint> _endPoints;

        /// <summary>
        /// it's save to iterate on this because it's replaced when a serving node is added or removed
        /// </summary>
        public IPEndPoint[] _safeEndPoints; 
        public readonly object _lock = new object();

        public void HandleServingNodesPayload(Span<byte> nodeEndPointsData)
        {
            for(int index = 0; index < nodeEndPointsData.Length; index +=6)
            {
                var endPoint = nodeEndPointsData.Slice(index).ParseIPEndPoint();
            }
        }

        void Add(IPEndPoint endPoint)
        {
            lock(_lock)
            {
                if(!_endPoints.Contains(endPoint))
                {
                    _endPoints.Add(endPoint);
                }
                _safeEndPoints = _endPoints.ToArray();
            }
        }

        public IPEndPoint[] EndPoints => _safeEndPoints;

        public void RemoveServingNode(IPEndPoint endPoint)
        {
            lock(_lock)
            {
                _endPoints.Remove(endPoint);
                _safeEndPoints = _endPoints.ToArray();
            }
        }
    }
}