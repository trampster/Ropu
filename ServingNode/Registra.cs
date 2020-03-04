using System;
using System.Collections.Generic;
using System.Net;
using Ropu.Shared.Groups;
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

        readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1,1);

        public async Task Register(Registration registration)
        {
            await Lock(async () =>
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
                            if(existingRegistration.EndPoint != registration.EndPoint)
                            {
                                foreach(var groupId in await _groupsClient.GetUsersGroups(registration.UserId))
                                {
                                    var endpoints = LookupGroupEndpoints(groupId);
                                    Console.WriteLine($"Changing endpoint for user: {registration.UserId} in group {groupId}");
                                    endpoints.Remove(existingRegistration.EndPoint);
                                    endpoints.Add(registration.EndPoint);
                                }
                            }
                        }
                    }
                }
                else
                {
                    _registrations.Add(registration);
                
                    foreach(var groupId in await _groupsClient.GetUsersGroups(registration.UserId))
                    {
                        var endpoints = LookupGroupEndpoints(groupId);
                        Console.WriteLine($"Adding user: {registration.UserId} to group {groupId}");
                        endpoints.Add(registration.EndPoint);
                    }
                }
            });
        }

        async Task Lock(Func<ValueTask> action)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                await action();
            }
            finally
            {
                _semaphoreSlim.Release();
            }  
        }

        async ValueTask<T> Lock<T>(Func<ValueTask<T>> action)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                _semaphoreSlim.Release();
            }  
        }

        SnapshotSet<IPEndPoint> LookupGroupEndpoints(ushort groupId)
        {
            if(_registeredGroupMembersLookup[groupId] == null)
            {
                Console.WriteLine($"Createing Group: {groupId}");
                _registeredGroupMembersLookup[groupId] = new SnapshotSet<IPEndPoint>(MaxGroupMembers);
            }
            return _registeredGroupMembersLookup[groupId];
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
                    await RemoveRegistration(registration);
                }
            }
        }

        public SnapshotSet<IPEndPoint> GetUserEndPoints(ushort groupId)
        {
            _semaphoreSlim.Wait();
            try
            {
                return LookupGroupEndpoints(groupId);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public bool UpdateRegistration(uint userId)
        {
            _semaphoreSlim.Wait();
            try
            {
                if(_registrationLookup.TryGetValue(userId, out var registration))
                {
                    registration?.Renew();
                    return true;
                }
                return false; //not registered
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        async Task RemoveRegistration(Registration registration)
        {
            _registrationLookup.Remove(registration.UserId);
            _registrations.Remove(registration);
            foreach(var groupId in await _groupsClient.GetUsersGroups(registration.UserId))
            {
                _registeredGroupMembersLookup[groupId].Remove(registration.EndPoint);
            }
        }

        public async Task Deregister(uint userId)
        {
            if(_registrationLookup.TryGetValue(userId, out var registration))
            {
                if(registration == null)
                {
                    return;
                }
                Console.WriteLine($"Deregister registration for User ID: {userId} at {registration.EndPoint}");
                await RemoveRegistration(registration);
            }
        }
    }
}