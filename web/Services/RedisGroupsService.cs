using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ropu.Shared.WebModels;
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


        public event EventHandler<(string name, ushort groupId)>? NameChanged;

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
                Id = (ushort)id,
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

        public (bool result, string message) Edit(Group group)
        {
            return _redisService.RunInTransaction("Failed to edit group", (db, transaction) =>
            {
                var groupsKey = $"Groups:{group.Id}";
                
                var existingGroupJson = db.StringGet(groupsKey);
                if(existingGroupJson.IsNull)
                {
                    return (false, "Failed to find group to edit");
                }

                var existingGroup = JsonConvert.DeserializeObject<RedisGroup>(existingGroupJson);

                transaction.AddCondition(Condition.KeyExists(groupsKey));
                var json = JsonConvert.SerializeObject(new RedisGroup()
                {
                    Id = group.Id,
                    Name = group.Name,
                    ImageHash = group.ImageHash,
                });
                transaction.StringSetAsync(groupsKey, json);

                if(existingGroup.Name != group.Name)
                {
                    var oldLookup = $"{GroupIdByNameKey}:{existingGroup.Name}";
                    transaction.AddCondition(Condition.KeyExists(oldLookup));
                    transaction.KeyDeleteAsync(oldLookup);

                    var newLookup = $"{GroupIdByNameKey}:{group.Name}";
                    transaction.AddCondition(Condition.KeyExists(newLookup));
                    transaction.StringSetAsync(newLookup, (uint)group.Id);

                    //sorted set (for paging)
                    long score = _redisService.CalculateStringScore(group.Name);
                    transaction.AddCondition(Condition.SortedSetContains(GroupsKey, (uint)group.Id));
                    transaction.SortedSetRemoveAsync(GroupsKey, (uint)group.Id);
                    transaction.SortedSetAddAsync(GroupsKey, (uint)group.Id, score);

                    NameChanged?.Invoke(this, (group.Name, (ushort)group.Id));
                }

                return (true, "");
            });
        }

        public (bool result, string message) Delete(uint id)
        {
            IDatabase db = _redisService.GetDatabase();

            var transaction = db.CreateTransaction();

            var groupsKey = $"Groups:{id}";
            
            var existingGroupJson = db.StringGet(groupsKey);
            if(existingGroupJson.IsNull)
            {
                return (false, "Failed to find group to edit");
            }

            var existingGroup = JsonConvert.DeserializeObject<RedisGroup>(existingGroupJson);

            transaction.AddCondition(Condition.KeyExists(groupsKey));
            transaction.KeyDeleteAsync(groupsKey);

            var idByNameKey = $"{GroupIdByNameKey}:{existingGroup.Name}";

            transaction.AddCondition(Condition.KeyExists(idByNameKey));
            transaction.KeyDeleteAsync(idByNameKey);

            transaction.AddCondition(Condition.SortedSetContains(GroupsKey, id));
            transaction.SortedSetRemoveAsync(GroupsKey, id);

            if(!transaction.Execute())
            {
                return (false, "Failed to delete group");
            }
            return (true, "");
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

        public IEnumerable<ushort> GroupIds
        {
            get
            {
                IDatabase db = _redisService.GetDatabase();
                foreach(int groupId in db.SortedSetRangeByScore("Groups"))
                {
                    yield return (ushort)groupId;
                }
            }
        }
    }
}