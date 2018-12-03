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

        public Registra(IGroupsClient groupsClient)
        {
            _groupsClient = groupsClient;
        }

        public void Register(Registration registration)
        {
            _registrationLookup.AddOrUpdate(registration.UserId, registration);
        }

        public Span<IPEndPoint> GetUserEndPoints(ushort groupId)
        {
            //TODO: we need maintain lists of group members which we update at registration time
            //doing a dictionary lookup for every single member will be to slow, ideally we would be
            //just returning an existing list so no allocation or looping is required
            var list = new List<IPEndPoint>();
            foreach(var userId in _groupsClient.Get(groupId).GroupMembers)
            {
                if(_registrationLookup.TryGetValue(userId, out Registration registration))
                {
                    list.Add(registration.EndPoint);
                }
            }
            return list.ToArray().AsSpan();
        }
    }
}