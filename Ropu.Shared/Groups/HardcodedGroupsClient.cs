using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Ropu.Shared.Groups
{
    public class HardcodedGroupsClient : IGroupsClient
    {
        public Dictionary<ushort, IGroup> _groupLookup;

        public HardcodedGroupsClient()
        {
            _groupLookup = new Dictionary<ushort, IGroup>();
            AddTestGroup(4242, "Avengers", 1000, 3000);
        }

        public IEnumerable<IGroup> Groups
        {
            get
            {
                return _groupLookup.Values;
            }
        }

        public int GroupCount => _groupLookup.Count;

        public IGroup Get(ushort groupId)
        {
            Console.WriteLine($"Getting Group {groupId}");
            if(_groupLookup.TryGetValue(groupId, out IGroup group))
            {
                return group;
            }
            return null;
        }

        public IEnumerable<ushort> GetUsersGroups(uint userId)
        {
            foreach(var group in _groupLookup)
            {
                if(group.Value.HasMember(userId))

                {
                    yield return group.Key;
                }
            }
        }

        void AddTestGroup(ushort groupId, string name, uint startUnitId, uint endUnitId)
        {
            var group = new Group(groupId);
            group.Name = name;

            for(uint unitId = startUnitId; unitId < endUnitId; unitId++)
            {
                group.Add(unitId);
            }
            _groupLookup.Add(groupId, group);
        }

        
    }
}