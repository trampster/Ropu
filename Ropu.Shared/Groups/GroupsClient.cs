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
        ushort[] _groupIds;

        public GroupsClient(RopuWebClient client)
        {
            _client = client;
        }
        
        public async Task<IEnumerable<ushort>> GetGroups()
        {
            if(_groupIds == null)
            {
                var response = await _client.Get<ushort[]>("api/Groups/GroupIds");
                if(response.StatusCode != HttpStatusCode.OK)
                {
                    Console.Error.WriteLine("Failed to get Groups from Web");
                    return null;
                }
                _groupIds = await response.GetJson();
            }
            return _groupIds;
        } 

        public async Task<Group> Get(ushort groupId)
        {
            var response = await _client.Get<Group>($"api/Groups/{groupId}");
            if(response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            var group = await response.GetJson();

            var imageResponse = await _client.Get<byte[]>($"api/Image/{group.ImageHash}");
            if(response.StatusCode == HttpStatusCode.OK)
            {
                group.Image = await imageResponse.GetBytes();
            }

            return group;
        }

        public async Task<ushort[]> GetUsersGroups(uint userId)
        {
            var response = await _client.Get<ushort[]>($"api/Users/{userId}/GroupIds");
            if(response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            return await response.GetJson();
        }
    }
}