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
        readonly RedisService _redisService;
        readonly PasswordHasher _passwordHasher;
        readonly IImageService _imageService;

        const string NextUserIdKey = "NextUserId";
        const string IdByEmailKey = "IdByEmail";
        const string UsersKey = "Users";

        public event EventHandler<(string name, uint userId)> NameChanged;

        public RedisUsersService(
            RedisService redisService, 
            PasswordHasher passwordHasher,
            IImageService imageService)
        {
            _redisService = redisService;
            _passwordHasher = passwordHasher;
            _imageService = imageService;
        }

        public (bool, string) AddUser(string name, string email, string password, List<string> roles)
        {
            IDatabase db = _redisService.GetDatabase();
            var transaction = db.CreateTransaction();
            var list = new List<ConditionResult>();
            (bool result, string message) = AddUser(db, transaction, list, name, email, password, roles);
            if(!result) return (result, message);
            if(!transaction.Execute())
            {
                return (false, "Failed to add user");
            }
            return (true, "");
        }

        (bool, string) AddUser(IDatabase db, ITransaction transaction, List<ConditionResult> conditionResults, string name, string email, string password, List<string> roles)
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

            //see if we already have it
            var idByEmailKey = $"{IdByEmailKey}:{email}";
            if(db.KeyExists(idByEmailKey))
            {
                return (false, "User already exists with that email");
            }

            conditionResults.Add(transaction.AddCondition(Condition.KeyNotExists(idByEmailKey)));

            //find the next id to use
            uint id = 0;
            if(!db.KeyExists(NextUserIdKey))
            {
                db.StringSetAsync(NextUserIdKey, id);
            }
            else
            {
                id = (uint)db.StringIncrement(NextUserIdKey); //this isn't in the transaction
            }

            // record the new id
            conditionResults.Add(transaction.AddCondition(Condition.KeyNotExists(idByEmailKey)));
            transaction.StringSetAsync(idByEmailKey, id);

            //add user table
            var usersKey = $"{UsersKey}:{id}";
            var user = new RedisUser()
            {
                Id = id, 
                Name=name,
                ImageHash=_imageService.DefaultUserImageHash,
                Roles = roles.ToList(),
                PasswordHash = _passwordHasher.HashPassword(password),
                Email = email
            };
            var json = JsonConvert.SerializeObject(user);
            transaction.StringSetAsync(usersKey, json);

            //add to sorted set (for paging)
            long score = _redisService.CalculateStringScore(name);
            conditionResults.Add(transaction.AddCondition(Condition.SortedSetNotContains(UsersKey, id)));
            transaction.SortedSetAddAsync(UsersKey, id, score);

            return (true, "");
        }

        public (bool, string) Edit(EditableUser user)
        {
            return _redisService.RunInTransaction("Failed to update user", (db, transaction) =>
            {
                var usersKey = $"Users:{user.Id}";

                var existingUserJson = db.StringGet(usersKey);
                if(existingUserJson.IsNull)
                {
                    return (false, "Failed to find user to edit");
                }
                var existingUser = JsonConvert.DeserializeObject<RedisUser>(existingUserJson);

                transaction.AddCondition(Condition.KeyExists(usersKey));
                var newUser = new RedisUser()
                {
                    Id = user.Id,
                    Name = user.Name,
                    ImageHash = user.ImageHash,
                    Email = user.Email,
                    Roles = user.Roles == null ? existingUser.Roles : user.Roles,
                    PasswordHash = user.Password == null ? 
                        existingUser.PasswordHash : 
                        _passwordHasher.HashPassword(user.Password)
                };
                var json = JsonConvert.SerializeObject(newUser);
                transaction.StringSetAsync(usersKey, json);

                if(existingUser.Name != newUser.Name)
                {
                    NameChanged?.Invoke(this, (newUser.Name, newUser.Id));
                }

                return ChangeEmail(db, transaction, existingUser, user);
            });
        }

        (bool, string) ChangeEmail(IDatabase db, ITransaction transaction, RedisUser existing, EditableUser edited)
        {           
            if(existing.Email == edited.Email)
            {
                return (true, ""); //not edited
            }

            //IdByEmail
            transaction.KeyDeleteAsync($"{IdByEmailKey}:{existing.Email}");
            transaction.StringSetAsync($"{IdByEmailKey}:{edited.Email}", existing.Id);

            return (true, "");
        }

        public IEnumerable<IUser> Users
        {
            get
            {
                IDatabase db = _redisService.GetDatabase();
                foreach(int userId in db.SortedSetRangeByScore("Users"))
                {
                    var user = db.StringGet($"{UsersKey}:{userId}");
                    yield return JsonConvert.DeserializeObject<User>(user);
                }
            }
        }

        public RedisUser AuthenticateUser(Credentials credentials)
        {
            IDatabase db = _redisService.GetDatabase();

            var idResult = db.StringGet($"{IdByEmailKey}:{credentials.Email}");
            if(idResult.IsNull)
            {
                return null;
            }
            uint id = (uint)idResult;

            var result = db.StringGet($"{UsersKey}:{id}");
            if(result.IsNull)
            {
                return null;
            }
            var user = JsonConvert.DeserializeObject<RedisUser>(result);
            
            if(_passwordHasher.VerifyHash(credentials.Password, user.PasswordHash))
            {
                user.PasswordHash = null; //they don't need it so best to limit access
                return user;
            }
            return null;
        }

        public IUser Get(uint userId)
        {
            IDatabase db = _redisService.GetDatabase();
            var user = db.StringGet($"{UsersKey}:{userId}");
            var redisUser = JsonConvert.DeserializeObject<RedisUser>(user);
            //clear sensitive information
            redisUser.PasswordHash = null;
            redisUser.Roles = null;
            redisUser.Email = null;
            return redisUser;
        }

        public EditableUser GetFull(uint id)
        {
            IDatabase db = _redisService.GetDatabase();
            var userJson = db.StringGet($"{UsersKey}:{id}");
            var redisUser = JsonConvert.DeserializeObject<RedisUser>(userJson);

            return new EditableUser()
            {
                Name = redisUser.Name,
                Email = redisUser.Email,
                ImageHash = redisUser.ImageHash,
                Id = redisUser.Id,
                Roles = redisUser.Roles
            };
        }

        public int Count()
        {
            IDatabase db = _redisService.GetDatabase();
            return (int)db.SortedSetLength(UsersKey);
        }
    }
}