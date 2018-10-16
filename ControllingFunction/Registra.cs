using System;
using System.Collections.Generic;
using System.Net;
using Ropu.Shared.Groups;
using System.Linq;

namespace Ropu.ControllingFunction
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

        public IPEndPoint GetEndPoint(uint userId)
        {
            if(!_registrationLookup.TryGetValue(userId, out Registration registration))
            {
                return null;
            }
            return registration.EndPoint;
        }

        public List<uint> RegisteredGroupMembers(ushort groupId)
        {
            var query = 
                from unitId in _groupsClient.Get(groupId).GroupMembers
                where _registrationLookup.ContainsKey(unitId)
                select unitId;
            return query.ToList();
        }

    }
}