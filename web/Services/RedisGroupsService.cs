using System.Collections.Generic;
using Newtonsoft.Json;
using Ropu.Web.Models;
using StackExchange.Redis;

namespace Ropu.Web.Services
{
    public class RedisGroupsService : IGroupsService
    {
        const string GroupIdByNameKey = "GroupIdByName";
        const string NextGroupIdKey = "NextGroupsId";
        const string GroupsKey = "Groups";

        readonly RedisService _redisService;
        readonly IImageService _imageService;


        public RedisGroupsService(
            RedisService redisService, 
            IImageService imageService)
        {
            _redisService = redisService;
            _imageService = imageService;
        }

        public (bool, string) AddGroup(string name, GroupType groupType)
        {
            IDatabase db = _redisService.GetDatabase();
            var transaction = db.CreateTransaction();
            (bool result, string message) = AddGroup(db, transaction, name, groupType);
            if(!result) return (result, message);
            if(!transaction.Execute())
            {
                return (false, "Failed to add Group");
            }
            return (true, "");
        }

        (bool, string) AddGroup(IDatabase db, ITransaction transaction, string name, GroupType groupType)
        {
            if(string.IsNullOrEmpty(name))
            {
                return (false, "Name is required");
            }
            var idByNameKey = $"{GroupIdByNameKey}:{name}";
            if(db.KeyExists(idByNameKey))
            {
                return (false, "User already exists with that email");
            }

            //find the next id to use
            uint id = 0;
            if(!db.KeyExists(NextGroupIdKey))
            {
                db.StringSetAsync(NextGroupIdKey, id);
            }
            else
            {
                id = (uint)db.StringIncrement(NextGroupIdKey); //this isn't in the transaction
            }
            
            // record id lookup by name
            transaction.AddCondition(Condition.KeyNotExists(idByNameKey));
            transaction.StringSetAsync(idByNameKey, id);

            // Add the group
            var usersKey = $"{GroupsKey}:{id}";
            var group = new RedisGroup()
            {
                Id = id,
                Name = name,
                GroupType = groupType,
                ImageHash = _imageService.DefaultGroupImageHash
            };
            var json = JsonConvert.SerializeObject(group);
            transaction.StringSetAsync(usersKey, json);

            //add to sorted set (for paging)
            long score = _redisService.CalculateStringScore(name);
            transaction.AddCondition(Condition.SortedSetNotContains(GroupsKey, id));
            transaction.SortedSetAddAsync(GroupsKey, id, score);

            return (true, "");
        }

        public IGroup Get(uint groupId)
        {
            IDatabase db = _redisService.GetDatabase();
            var group = db.StringGet($"{GroupsKey}:{groupId}");
            return JsonConvert.DeserializeObject<RedisGroup>(group);
        }

        public IEnumerable<IGroup> Groups
        {
            get
            {
                IDatabase db = _redisService.GetDatabase();
                foreach(int groupId in db.SortedSetRangeByScore("Groups"))
                {
                    var group = db.StringGet($"{GroupsKey}:{groupId}");
                    yield return JsonConvert.DeserializeObject<RedisGroup>(group);
                }
            }
        }
    }
}