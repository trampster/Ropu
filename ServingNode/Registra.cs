using System;
using System.Collections.Generic;
using System.Net;
using Ropu.Shared.Groups;
using System.Linq;

namespace Ropu.ServingNode
{
    public class Registra
    {
        readonly Dictionary<uint, Registration> _registrationLookup = new Dictionary<uint, Registration>();
        readonly IGroupsClient _groupsClient;

        public Registra(IGroupsClient groupsClient)
        {
            _groupsClient = groupsClient;
        }

        public void Register(Registration registration)
        {
            if(_registrationLookup.TryGetValue(registration.UserId, out Registration existing))
            {
                //update 
                _registrationLookup[registration.UserId] = registration;
                return;
            }
            _registrationLookup.Add(registration.UserId, registration);
        }

        public List<uint> GetUsers(ushort groupId)
        {
            //TODO: we need maintain lists of group members which we update at registration time
            //doing a dictionary lookup for every single member will be to slow, ideally we would be
            //just returning an existing list so no allocation or looping is required
            var query = 
                from userId in _groupsClient.Get(groupId).GroupMembers
                where _registrationLookup.ContainsKey(userId)
                select userId;
            return query.ToList();
        }

    }
}