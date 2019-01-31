using System;
using System.Collections.Generic;
using System.Net;
using Ropu.Shared.Groups;
using System.Linq;
using Ropu.Shared.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace Ropu.ServingNode
{
    public class Registra
    {
        readonly IntDictionary<Registration> _registrationLookup = new IntDictionary<Registration>();
        readonly List<Registration> _registrations = new List<Registration>();
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
            Console.WriteLine($"Received registration from User ID: {registration.UserId} at {registration.EndPoint}");

            registration.Renew();
            if(_registrationLookup.AddOrUpdate(registration.UserId, registration))
            {
                //was an update, need to change the one in the list
                for(int index = 0; index < _registrations.Count; index++)
                {
                    var existingRegistration = _registrations[index];
                    if(existingRegistration.UserId == registration.UserId)
                    {
                        _registrations[index] = registration;
                    }
                }
            }
            else
            {
                _registrations.Add(registration);
            }
            foreach(var groupId in _groupsClient.GetUsersGroups(registration.UserId))
            {
                if(_registeredGroupMembersLookup[groupId] == null)
                {
                    _registeredGroupMembersLookup[groupId] = new SnapshotSet<IPEndPoint>(MaxGroupMembers);
                }
                _registeredGroupMembersLookup[groupId].Add(registration.EndPoint);
            }
        }

        public async Task CheckExpiries()
        {
            var thirtySeconds = new TimeSpan(0,0,30);
            var toRemove = new List<Registration>();

            while(true)
            {
                await Task.Delay(30000);
                var oldestToKeep = DateTime.UtcNow.Subtract(thirtySeconds);
                toRemove.Clear();
                foreach(var registration in _registrations)
                {
                    if(registration.LastSeen < oldestToKeep)
                    {
                        toRemove.Add(registration);
                    }
                }
                foreach(var registration in toRemove)
                {
                    Console.WriteLine($"Removing expired registration for User ID: {registration.UserId} at {registration.EndPoint}");
                    _registrationLookup.Remove(registration.UserId);
                    _registrations.Remove(registration);
                }
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

        public bool UpdateRegistration(uint userId)
        {
            if(_registrationLookup.TryGetValue(userId, out var registration))
            {
                registration.Renew();
                return true;
            }
            return false; //not registered
        }

        public void Deregister(uint userId)
        {
            if(_registrationLookup.TryGetValue(userId, out var registration))
            {
                Console.WriteLine($"Deregister registration for User ID: {userId} at {registration.EndPoint}");

                _registrationLookup.Remove(userId);
                _registrations.Remove(registration);
            }
        }
    }
}