using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared.Web;
using Ropu.Shared.Web.Models;
using Ropu.Shared.WebModels;

namespace Ropu.Shared.Groups
{
    public class UsersClient : IUsersClient
    {
        readonly RopuWebClient _client;

        public UsersClient(RopuWebClient client)
        {
            _client = client;
        }

        public async Task<IUser?> Get(uint userId)
        {
            var response = await _client.Get<User>($"api/Users/{userId}");
            if(response.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"Failed to get user {userId}");
                return null;
            }
            return await response.GetJson();
        }

        public async Task<IUser?> GetCurrentUser()
        {
            var response = await _client.Get<User>($"api/Users/Current").ConfigureAwait(false);
            if(response.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"Failed to get current user");
                return null;
            }
            return await response.GetJson().ConfigureAwait(false);
        }

        public async ValueTask<bool> Create(NewUser newUser)
        {
            var response = await _client.Post($"api/users/create", newUser);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            Console.Error.WriteLine($"Failed to create user {newUser.Name}");
            return false;
        }
    }
}