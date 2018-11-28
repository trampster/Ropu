using System.Net;

namespace Ropu.ServingNode
{
    public class GroupCallControllerLookup
    {
        readonly IPEndPoint[] _lookup = new IPEndPoint[ushort.MaxValue];
        public void Add(ushort groupId, IPEndPoint endPoint)
        {
            _lookup[groupId] = endPoint;
        }

        public void Remove(ushort groupId)
        {
            _lookup[groupId] = null;
        }

        public IPEndPoint LookupEndPoint(ushort groupId)
        {
            return _lookup[groupId];
        }
    }
}