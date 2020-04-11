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
        readonly Dictionary<uint, List<CachedEncryptionKey>> _users = new Dictionary<uint, List<CachedEncryptionKey>>();
        readonly Dictionary<uint, List<CachedEncryptionKey>> _groups = new Dictionary<uint, List<CachedEncryptionKey>>();
        readonly List<CachedEncryptionKey> _myKeys = new List<CachedEncryptionKey>();
        readonly RopuWebClient _ropuWebClient;
        readonly Func<EncryptionKey, CachedEncryptionKey> _cachedEncryiptonKeyFactory;

        readonly bool _cacheAllServices;

        public KeysClient(RopuWebClient ropuWebClient, bool cacheAllServices, Func<EncryptionKey, CachedEncryptionKey> cachedEncryiptonKeyFactory)
        {
            _ropuWebClient = ropuWebClient;
            _cacheAllServices = cacheAllServices;
            _cachedEncryiptonKeyFactory = cachedEncryiptonKeyFactory;
        }

        public async Task ExpireKey(CancellationToken cancellationToken)
        {
            List<uint> toRemove = new List<uint>();
            while(!cancellationToken.IsCancellationRequested)
            {
                removeExpired(_users, toRemove);
                removeExpired(_groups, toRemove);
                await Task.Delay((int)TimeSpan.FromDays(1).TotalMilliseconds);
            }
        }

        void removeExpired(Dictionary<uint, List<CachedEncryptionKey>> cache, List<uint> toRemove)
        {
            toRemove.Clear();
            foreach(uint userId in _users.Keys)
            {
                var keys = cache[userId];
                if(isExpired(keys))
                {
                    toRemove.Add(userId);
                }
            }
            foreach(var userId in toRemove)
            {
                cache.Remove(userId);
            }
        }

        bool isExpired(List<CachedEncryptionKey> keys)
        {
            return !keys.Any(key => key.isTodaysKey());
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                if(!await CacheMyKeys())
                {
                    Console.Error.WriteLine("Failed to cache my keys");
                    await Task.Delay(5000, cancellationToken);
                    continue;
                }
                if(_cacheAllServices)
                {
                    if(!await CacheServices())
                    {
                        await Task.Delay(5000, cancellationToken);
                        continue;
                    }
                }
                await Task.Delay(12 * 60 * 60 * 1000, cancellationToken);
            }
        }

        public async Task<CachedEncryptionKey?> GetKey(bool isUser, uint sourceId)
        {
            if(isUser)
            {
                return await GetUserKey(sourceId);
            }
            return await GetGroupKey(sourceId);
        }

        public async Task<CachedEncryptionKey?> GetKey(bool isUser, uint sourceId, byte keyId)
        {
            if(isUser)
            {
                return await GetUserKey(sourceId, keyId);
            }
            return await GetGroupKey(sourceId, keyId);
        }

        public AesGcmEncryption? GetMyKey()
        {
            return GetTodaysEncryption(_myKeys);
        }

        public CachedEncryptionKey? GetMyKeyInfo()
        {
            return GetTodaysEncryptionInfo(_myKeys);
        }

        public async Task<CachedEncryptionKey?> GetUserKey(uint userId)
        {
            CachedEncryptionKey? encryption;
            if(_users.TryGetValue(userId, out var keys))
            {
                encryption = GetTodaysEncryptionInfo(keys);
                if(encryption != null)
                {
                    return encryption;
                }
                //keys are all expired
                _users.Remove(userId);
            }

            var keys1 = await RefreshUsersKeys(userId);
            if(keys1 == null) return null;

            return GetTodaysEncryptionInfo(keys1);
        }

        async Task<List<CachedEncryptionKey>?> RefreshUsersKeys(uint userId)
        {
            var response = await _ropuWebClient.Get<List<EncryptionKey>>($"api/Key/False/{userId}");
            if(!response.IsSuccessfulStatusCode)
            {
                Console.Error.WriteLine($"Failed to find a key for user with UserId {userId}");
                return null;
            }
            var keys = (await response.GetJson()).Select(key => _cachedEncryiptonKeyFactory(key)).ToList();
            _users.Remove(userId);
            _users.Add(userId, keys);
            return keys;
        }

        public async Task<CachedEncryptionKey?> GetUserKey(uint userId, byte keyId)
        {
            CachedEncryptionKey? encryption;
            if(_users.TryGetValue(userId, out var keys))
            {
                encryption = GetEncryptionInfo(keys, keyId);
                if(encryption == null)
                {
                    //no match for keyId, needs to get them again.
                    _users.Remove(userId);
                }
                else
                {
                    return encryption;
                }
            }

            var keys1 = await RefreshUsersKeys(userId);
            if(keys1 == null) return null;

            return GetEncryptionInfo(keys1, keyId);
        }

        async Task<List<CachedEncryptionKey>?> RefreshGroupKeys(uint groupId)
        {
            Console.WriteLine($"KeysClient: RefreshGroupKeys groupId: {groupId}");
            var response = await _ropuWebClient.Get<List<EncryptionKey>>($"api/Key/True/{groupId}");
            if(!response.IsSuccessfulStatusCode)
            {
                Console.Error.WriteLine($"Failed to get a key for group with GroupId {groupId} with response code {response.StatusCode}");
                return null;
            }
            var keys = (await response.GetJson()).Select(key => _cachedEncryiptonKeyFactory(key)).ToList();
            _groups.TryAdd(groupId, keys);
            return keys;
        }

        public async Task<CachedEncryptionKey?> GetGroupKey(uint groupId)
        {
            CachedEncryptionKey? encryption;
            if(_groups.TryGetValue(groupId, out var keys))
            {
                encryption = GetTodaysEncryptionInfo(keys);
                if(encryption != null)
                {
                    return encryption;
                }
                //keys are all expired
                _groups.Remove(groupId);
            }

            var keys1 = await RefreshGroupKeys(groupId);
            if(keys1 == null) return null;

            return GetTodaysEncryptionInfo(keys1);
        }

        public async Task<CachedEncryptionKey?> GetGroupKey(uint groupId, byte keyId)
        {
            CachedEncryptionKey? encryption;
            if(_groups.TryGetValue(groupId, out var keys))
            {
                encryption = GetEncryptionInfo(keys, keyId);
                if(encryption != null)
                {
                    return encryption;    
                }
                //keys are all expired
                _groups.Remove(groupId);
            }

            var keys1 = await RefreshGroupKeys(groupId);
            if(keys1 == null) return null;

            return GetEncryptionInfo(keys1, keyId);
        }

        AesGcmEncryption? GetTodaysEncryption(List<CachedEncryptionKey> keys)
        {
            foreach(var key in keys)
            {
                if(key.isTodaysKey())
                {
                    return key.Encryption;
                }
            }
            return null;
        }

        CachedEncryptionKey? GetEncryptionInfo(List<CachedEncryptionKey> keys, byte keyId)
        {
            foreach(var key in keys)
            {
                if(key.KeyId == keyId)
                {
                    return key;
                }
            }
            Console.Error.WriteLine($"None of the keys match keyId {keyId}");
            return null;
        }

        CachedEncryptionKey? GetTodaysEncryptionInfo(List<CachedEncryptionKey> keys)
        {
            foreach(var key in keys)
            {
                if(key.isTodaysKey())
                {
                    return key;
                }
            }
            Console.Error.WriteLine("None of the keys are for today");
            return null;
        }

        async ValueTask<bool> CacheMyKeys()
        {
            var response = await _ropuWebClient.Get<List<EncryptionKey>>($"api/Key/MyKeys");
            if(!response.IsSuccessfulStatusCode)
            {
                Console.Error.WriteLine($"Failed to get my keys with reponse code {response.StatusCode}");
                return false;
            }
            _myKeys.Clear();
            var keys = (await response.GetJson()).Select(key => _cachedEncryiptonKeyFactory(key));
            _myKeys.AddRange(keys);
            return true;
        }

        public async Task WaitForkeys()
        {
            while(true)
            {
                if(_myKeys.Count > 0)
                {
                    return;
                }
                await Task.Delay(1000);
            }
        }

        async ValueTask<bool> CacheServices()
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
                Console.Error.WriteLine($"Failed to get service keys, with response {keysResponse.StatusCode}");
                return false;
            }

            var entities = await keysResponse.GetJson();

            foreach(var entity in entities)
            {
                var keys = entity.Keys.Select(key => _cachedEncryiptonKeyFactory(key)).ToList();
                if(!_users.TryAdd(entity.UserOrGroupId, keys))
                {
                    _users[entity.UserOrGroupId] = keys;
                }
            }
            return true;
        }
    }
}