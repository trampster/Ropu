using System;
using System.Collections.Generic;
using System.Net;
using Ropu.Shared.Groups;
using System.Linq;
using Ropu.Shared.Concurrent;

namespace Ropu.ServingNode
{
    public class Registra
    {
        readonly IntDictionary<Registration> _registrationLookup = new IntDictionary<Registration>();
        readonly IGroupsClient _groupsClient;
        readonly SnapshotSet<IPEndPoint>[] _registeredGroupMembersLookup;
        const int MaxGroupMembers = 2000;

        public Registra(IGroupsClient groupsClient)
        {
            _groupsClient = groupsClient;
            _registeredGroupMembersLookup = new SnapshotSet<IPEndPoint>[ushort.MaxValue];
        }

        public void Register(Registration registration)
        {
            _registrationLookup.AddOrUpdate(registration.UserId, registration);
            foreach(var groupId in _groupsClient.GetUsersGroups(registration.UserId))
            {
                if(_registeredGroupMembersLookup[groupId] == null)
                {
                    _registeredGroupMembersLookup[groupId] = new SnapshotSet<IPEndPoint>(MaxGroupMembers);
                }
                _registeredGroupMembersLookup[groupId].Add(registration.EndPoint);
            }
        }

        public ISetReader<IPEndPoint> GetUserEndPoints(ushort groupId)
        {
            var snapshotSet = _registeredGroupMembersLookup[groupId];
            if(snapshotSet == null)
            {
                return null;
            }
            return snapshotSet.GetSnapShot();
        }
    }
}