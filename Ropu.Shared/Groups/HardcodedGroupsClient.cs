using System.Collections.Generic;
using System.Net;

namespace Ropu.Shared.Groups
{
    public class HardcodedGroupsClient : IGroupsClient
    {
        public Dictionary<ushort, List<IPEndPoint>> _groupLookup;

        public HardcodedGroupsClient()
        {
            _groupLookup = new Dictionary<ushort, List<IPEndPoint>>();
            AddTestGroup();
        }

        void AddTestGroup()
        {
            var endpoints = new List<IPEndPoint>();
            for(int endpointIndex = 0; endpointIndex < 1000; endpointIndex++)
            {
                endpoints.Add(new IPEndPoint(IPAddress.Parse("192.168.1.2"), endpointIndex + 1000));
            }
            endpoints.Add(new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5061));

            _groupLookup.Add(4242, endpoints);
        }

        public List<IPEndPoint> GetGroupMemberEndpoints(ushort groupId)
        {
            if(!_groupLookup.TryGetValue(groupId, out List<IPEndPoint> endpoints))
            {
                return null;
            }
            return endpoints;
        }
    }
}