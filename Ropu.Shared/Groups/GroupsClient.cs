using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ropu.Shared.Web;

namespace Ropu.Shared.Groups
{
    public class GroupsClient : IGroupsClient
    {
        readonly RopuWebClient _client;
        readonly ImageClient _imageClient;
        ushort[] _groupIds = new ushort[0];
        bool _haveGroupsIds = false;

        readonly Dictionary<ushort, (Group, DateTime)> _groupsCache = new Dictionary<ushort, (Group, DateTime)>();

        public GroupsClient(RopuWebClient client, ImageClient imageClient)
        {
            _client = client;
            _imageClient = imageClient;
        }
        
        public async Task<IEnumerable<ushort>> GetGroups()
        {
            if(!_haveGroupsIds)
            {
                var response = await _client.Get<ushort[]>("api/Groups/GroupIds");
                if(response.StatusCode != HttpStatusCode.OK)
                {
                    Console.Error.WriteLine("Failed to get Groups from Web");
                    return _groupIds;
                }
                _groupIds = await response.GetJson();
            }
            return _groupIds;
        } 

        public async Task<Group?> Get(ushort groupId)
        {
            if(_groupsCache.TryGetValue(groupId, out (Group, DateTime) cachedGroup))
            {
                if(cachedGroup.Item2 > DateTime.UtcNow)
                {
                    //not expired
                    return cachedGroup.Item1;
                }
            }
            var response = await _client.Get<Group>($"api/Groups/{groupId}");
            if(response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            var group = await response.GetJson();

            group.Image = await _imageClient.GetImage(group.ImageHash);

            _groupsCache[groupId] = (group, DateTime.UtcNow + TimeSpan.FromMinutes(5));
            return group;
        }

        public async Task<ushort[]> GetUsersGroups(uint userId)
        {
            var response = await _client.Get<ushort[]>($"api/Users/{userId}/GroupIds");
            if(response.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine("Failed to get Users Groups from Web");
                return new ushort[0];
            }

            return await response.GetJson();
        }

        ushort[]? _myGroups;
        DateTime _myGroupsTime = DateTime.UnixEpoch;
        

        public async Task<ushort[]> GetMyGroups(uint myUserId)
        {
            if(_myGroups != null && _myGroupsTime.AddSeconds(30) > DateTime.UtcNow)
            {
                return _myGroups;
            }

            var response = await _client.Get<ushort[]>($"api/Users/{myUserId}/GroupIds");
            if(response.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine("Failed to get Users Groups from Web");
                return new ushort[0];
            }

            _myGroups = await response.GetJson();
            _myGroupsTime = DateTime.UtcNow;
            return _myGroups;
        }

        public async Task<bool> Join(ushort groupId, uint userId)
        {
            var response = await _client.Post<string>($"api/Groups/{groupId}/Join/{userId}", ""); 
            if(!response.IsSuccessful)
            {
                Console.Error.WriteLine($"Failed to join group {groupId} with reason {response.FailureReason}");
                return false;
            }
            _myGroups = null; //resets cache so we will git it fresh next time
            return true;      
        }

        public async Task<bool> Leave(ushort groupId, uint userId)
        {
            var response = await _client.Delete($"api/Groups/{groupId}/Leave/{userId}"); 
            if(!response.IsSuccessful)
            {
                Console.Error.WriteLine($"Failed to leave group {groupId} with reason {response.FailureReason}");
                return false;
            }
            _myGroups = null; //resets cache so we will git it fresh next time
            return true;      
        }
    }
}