using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Newtonsoft.Json;
using Ropu.Web.Models;
using StackExchange.Redis;

namespace Ropu.Web.Services
{
    public class RedisUsersService : IUsersService
    {
        readonly ConnectionMultiplexer _connectionMultiplexer;
        public RedisUsersService(ConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;

            AddUsers();
        }

        void AddUsers()
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();

            foreach(var user in _users)
            {
                var key = $"Users:{user.Id}";
                if(db.KeyExists(key))
                {
                    continue;
                }
                var json = JsonConvert.SerializeObject(user);

                db.StringSet(key, json);
                
                var name = user.Name;
                double score = 
                    (name[0] << 48) + 
                    (name[1] << 32) + 
                    (name[2] << 16) + 
                    (name[3]);
                db.SortedSetAdd("Users", json, score);
            }
        }

        readonly IUser[] _users = new[]
        {
            new User()
            {
                Name = "Batmap",
                Id = 1
            },
            new User()
            {
                Name = "Superman",
                Id = 2
            }
        };

        public IEnumerable<IUser> Users
        {
            get
            {
                IDatabase db = _connectionMultiplexer.GetDatabase();
                foreach(var user in db.SortedSetRangeByScore("Users"))
                {
                    yield return JsonConvert.DeserializeObject<User>(user);
                }
            }
        }
    }
}