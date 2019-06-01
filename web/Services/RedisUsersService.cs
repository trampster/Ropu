using System;
using System.Collections.Generic;
using System.Linq;
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
        readonly PasswordHasher _passwordHasher;
        public RedisUsersService(ConnectionMultiplexer connectionMultiplexer, PasswordHasher passwordHasher)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _passwordHasher = passwordHasher;

            AddUsers();
        }

        void AddUsers()
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();

            AddUser("Batmap", "batmap", "password1", new []{"Admin"});
            AddUser("Superman", "soups", "password2", new []{"Admin"});
            AddUser("Green Lantin", "greenl", "password3", new []{"Admin"});
            AddUser("Flash", "flash", "password4", new []{"Admin"});
            AddUser("Wonder Woman", "wonder", "password5", new []{"Admin"});
        }

        public void AddUser(string name, string username, string password, string[] roles)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();

            //see if we already have it
            var idByUsernameKey = $"IdByUsername:{username}";
            if(db.KeyExists(idByUsernameKey))
            {
                return;
            }

            //find the next id to use
            const string nextUserIdKey = "NextUserId";
            if(!db.KeyExists(nextUserIdKey))
            {
                db.StringSet(nextUserIdKey, 0);
            }
            int id = (int)db.StringGet(nextUserIdKey);
            db.StringIncrement(nextUserIdKey);

            // record the new id
            db.StringSet(idByUsernameKey, id);

            //add user table
            var usersKey = $"Users:{id}";
            var user = new User(){Id = id, Name=name};
            var json = JsonConvert.SerializeObject(user);
            db.StringSet(usersKey, json);

            //add to sorted set (for paging)
            var lowerName = name.ToLowerInvariant();
            long score = 
                ((long)lowerName[0] << 48) + 
                ((long)lowerName[1] << 32) + 
                ((long)lowerName[2] << 16) + 
                ((long)lowerName[3]);
            db.SortedSetAdd("Users", id, score);

            //add to user credentials
            var userCredentials = new UserCredentials()
            {
                Id = id,
                UserName = username,
                PasswordHash = _passwordHasher.HashPassword(password),
                Roles = roles.ToList()
            };
            var userCredentialsKey = $"UsersCredentials:{userCredentials.UserName}";
            db.StringSet(userCredentialsKey, JsonConvert.SerializeObject(userCredentials));
        }

        public IEnumerable<IUser> Users
        {
            get
            {
                IDatabase db = _connectionMultiplexer.GetDatabase();
                foreach(int userId in db.SortedSetRangeByScore("Users"))
                {
                    var user = db.StringGet($"Users:{userId}");
                    yield return JsonConvert.DeserializeObject<User>(user);
                }
            }
        }

        public UserCredentials AuthenticateUser(Credentials credentials)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var result = db.StringGet($"UsersCredentials:{credentials.UserName}");
            if(result.IsNull)
            {
                return null;
            }
            var userCredentials = JsonConvert.DeserializeObject<UserCredentials>(result);
            
            if(_passwordHasher.VerifyHash(credentials.Password, userCredentials.PasswordHash))
            {
                userCredentials.PasswordHash = null; //they don't need it so best to limit access
                return userCredentials;
            }
            return null;
        }
    }
}