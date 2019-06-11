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
        readonly IImageService _imageService;

        public RedisUsersService(
            ConnectionMultiplexer connectionMultiplexer, 
            PasswordHasher passwordHasher,
            IImageService imageService)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _passwordHasher = passwordHasher;
            _imageService = imageService;

            AddUsers();
        }

        void AddUsers()
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();

            AddUser("Batmap", "batmap@dc.com", "password1", new []{"Admin"});
            AddUser("Superman", "souperman@dc.com", "password2", new []{"Admin"});
            AddUser("Green Lantin", "green.lantin@dc.com", "password3", new []{"Admin"});
            AddUser("Flash", "flash", "password4@dc.com", new []{"Admin"});
            AddUser("Wonder Woman", "wonder.woman@dc.com", "password5", new []{"Admin"});
        }

        public (bool, string) AddUser(string name, string email, string password, string[] roles)
        {
            if(string.IsNullOrEmpty(name))
            {
                return (false, "Name is required");
            }
            if(string.IsNullOrEmpty(email))
            {
                return (false, "Email is required");
            }
            if(!email.Contains('@'))
            {
                return (false, "Email is invalid");
            }
            if(roles == null)
            {
                return (false, "Email is invalid");
            }

            IDatabase db = _connectionMultiplexer.GetDatabase();

            //see if we already have it
            var idByUsernameKey = $"IdByEmail:{email}";
            if(db.KeyExists(idByUsernameKey))
            {
                return (false, "User already exists with that email");
            }

            //find the next id to use
            const string nextUserIdKey = "NextUserId";
            if(!db.KeyExists(nextUserIdKey))
            {
                db.StringSet(nextUserIdKey, 0);
            }
            uint id = (uint)db.StringGet(nextUserIdKey);
            db.StringIncrement(nextUserIdKey);

            // record the new id
            db.StringSet(idByUsernameKey, id);

            //add user table
            var usersKey = $"Users:{id}";
            var user = new User()
            {
                Id = id, 
                Name=name,
                ImageHash=_imageService.DefaultUserImageHash
            };
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
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password),
                Roles = roles.ToList()
            };
            var userCredentialsKey = $"UsersCredentials:{userCredentials.Email}";
            db.StringSet(userCredentialsKey, JsonConvert.SerializeObject(userCredentials));

            return (true, "");
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
            var result = db.StringGet($"UsersCredentials:{credentials.Email}");
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

        public IUser Get(uint userId)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var user = db.StringGet($"Users:{userId}");
            return JsonConvert.DeserializeObject<User>(user);
        }
    }
}