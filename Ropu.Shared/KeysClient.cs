using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared.Web;
using Ropu.Shared.WebModels;

namespace Ropu.Shared
{
    public class KeysClient
    {
        readonly Dictionary<uint, List<EncryptionKey>> _services = new Dictionary<uint, List<EncryptionKey>>();
        readonly Dictionary<uint, List<EncryptionKey>> _users = new Dictionary<uint, List<EncryptionKey>>();
        readonly Dictionary<uint, List<EncryptionKey>> _groups = new Dictionary<uint, List<EncryptionKey>>();
        readonly RopuWebClient _ropuWebClient;

        public KeysClient(RopuWebClient ropuWebClient)
        {
            _ropuWebClient = ropuWebClient;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                if(!await CacheServices())
                {
                    await Task.Delay(5000, cancellationToken);
                }
                await Task.Delay(12 * 60 * 60 * 1000, cancellationToken);
            }
        }

        public byte[]? GetServiceKey(uint userId)
        {
            if(_services.TryGetValue(userId, out var keys))
            {
                foreach(var key in keys)
                {
                    if(key.Date.Date == DateTime.UtcNow.Date)
                    {
                        return key.GetKeyMaterial();
                    }
                }
            }
            Console.Error.WriteLine($"Couldn't find a key for service with UserId {userId}");
            return null;
        }

        async Task<bool> CacheServices()
        {
            //TODO: need to get service userIds first then get the keys for that service
            var response = await _ropuWebClient.Get<List<ServiceInfo>>("api/Services/All");      
            if(response.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine("Failed to get services list for cache keys");
                return false;
            }
            var services = await response.GetJson();

            var keysResponse = await _ropuWebClient.Post<IEnumerable<uint>, List<EntitiesKeys>>(
                "api/Key/UsersKeys",
                services.Select(s => s.UserId));

            if(keysResponse.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine("Failed to get service keys");
                return false;
            }

            var entities = await keysResponse.GetJson();

            _services.Clear();
            foreach(var entity in entities)
            {
                _services.Add(entity.UserOrGroupId, entity.Keys);
            }
            return true;
        }
    }
}