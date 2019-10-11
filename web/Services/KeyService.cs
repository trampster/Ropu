using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Ropu.Shared.WebModels;
using StackExchange.Redis;

namespace Ropu.Web.Services
{
    public class KeyService
    {
        readonly RedisService _redisService;
        
        public KeyService(RedisService redisService)
        {
            _redisService = redisService;
            _redisService.KeyDeleted += (sender, args) => KeyDeleted(args);
        }

        void KeyDeleted(string key)
        {
            if(key.StartsWith("Users:"))
            {
                //delete keys for user
                IDatabase db = _redisService.GetDatabase();

                var userId = uint.Parse(key.AsSpan().Slice(key.IndexOf(':') + 1));
                db.KeyDelete(GetKey(false, userId));
            }
            else if(key.StartsWith("Groups:"))
            {
                //delete keys for user
                IDatabase db = _redisService.GetDatabase();

                var groupId = uint.Parse(key.AsSpan().Slice(key.IndexOf(':') + 1));
                db.KeyDelete(GetKey(true, groupId));
            }
        }

        string GetKey(bool isGroup, uint userOrGroupId)
        {
            int type = isGroup ? 0 : 1;
            return $"EncryptionKeys:{type}{userOrGroupId}";
        }

        public List<EncryptionKey> GetKeys(bool isGroup, uint userOrGroupId)
        {
            //TODO: check that group or user exists
            IDatabase db = _redisService.GetDatabase();
            var encryptionKeysKey = GetKey(isGroup, userOrGroupId);

            var keysJson = db.StringGet(encryptionKeysKey);
            List<EncryptionKey>? keys;
            if(!keysJson.IsNull)
            {
                keys = JsonConvert.DeserializeObject<List<EncryptionKey>>(keysJson);
            }
            else
            {
                keys = new List<EncryptionKey>();
            }

            UpdateKeyList(keys);
            
            db.StringSet(encryptionKeysKey, JsonConvert.SerializeObject(keys), expiry: new TimeSpan(2,0,0,0));

            return keys;
        }

        void UpdateKeyList(List<EncryptionKey> keys)
        {
            var now = DateTime.UtcNow.Date;
            var cutoff = now.Subtract(new TimeSpan(1,0,0,0));
            keys.RemoveAll(key => key.Date < cutoff);


            int startId = 0;
            DateTime startDate = now;
            if(keys.Count > 0)
            {
                var last = keys.Last();
                startId = last.KeyId + 1;
                startDate = last.Date.AddDays(1);
            }

            foreach(int index in Enumerable.Range(0, 3 - keys.Count))
            {
                keys.Add(new EncryptionKey()
                {
                    KeyId = (startId + index) % 3,
                    Date = startDate.AddDays(index),
                    KeyMaterial = ToHex(GenerateKey())
                });
            }
        }

        string ToHex(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        byte[] GenerateKey()
        {
            var aes = new AesCryptoServiceProvider();
            aes.KeySize = 256;
            aes.GenerateKey();
            return aes.Key;
        }
    }
}