using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Ropu.Shared.WebModels;
using StackExchange.Redis;

namespace Ropu.Web.Services
{
    public class GroupMembersipService
    {
        readonly IUsersService _usersService;
        readonly IGroupsService _groupsService;
        readonly RedisService _redisService;
        readonly ILogger _logger;

        public GroupMembersipService(
            IUsersService usersService, 
            IGroupsService groupsService, 
            RedisService redisService,
            ILogger<GroupMembersipService> logger)
        {
            _logger = logger;
            _usersService = usersService;
            _groupsService = groupsService;
            _redisService = redisService;
            _usersService.NameChanged += (sender, args) => ChangeUserName(args.userId, args.name);
            _groupsService.NameChanged += (sender, args) => ChangeGroupName(args.groupId, args.name);
        }

        public (bool result, string message) RemoveGroupMember(ushort groupId, uint userId)
        {
            return _redisService.RunInTransaction("Failed remove group member", (db, transaction) =>
            {
                //add to group members
                transaction.SortedSetRemoveAsync(GroupMembersKey(groupId), userId);


                // add to users groups
                transaction.SortedSetRemoveAsync(UsersGroupsKey(userId), (int)groupId);

                return (true, "");
            });
        }

        public (bool result, string message) AddGroupMember(ushort groupId, uint userId)
        {
            return _redisService.RunInTransaction("Failed add group member", (db, transaction) =>
            {
                //lookup names
                var userName = _usersService.Get(userId).Name;
                var groupName = _groupsService.Get(groupId).Name;

                //add to group members
                long userNameScore = _redisService.CalculateStringScore(userName);
                var groupMembersKey = GroupMembersKey(groupId);
                transaction.AddCondition(Condition.SortedSetNotContains(groupMembersKey, userId));
                transaction.SortedSetAddAsync(groupMembersKey, userId, userNameScore);

                // add to users groups
                long groupNameScore = _redisService.CalculateStringScore(userName);
                var usersGroupsKey = UsersGroupsKey(userId);
                transaction.AddCondition(Condition.SortedSetNotContains(usersGroupsKey, (int)groupId));
                transaction.SortedSetAddAsync(usersGroupsKey, (int)groupId, groupNameScore);

                return (true, "");
            });
        }

        string UsersGroupsKey(uint userId) => $"UsersGroups:{userId}";
        string GroupMembersKey(ushort groupId) => $"GroupMembers:{groupId}";

        void ChangeUserName(uint userId, string newName)
        {
            var db = _redisService.CurrentDatabase;
            if(db == null)
            {
                throw new InvalidOperationException("No current database");
            }
            var transaction = _redisService.CurrentTransaction;
            //find what groups the user is in
            foreach(int groupId in db.SortedSetRangeByScore(UsersGroupsKey(userId)))
            {
                var key = GroupMembersKey((ushort)groupId);
                transaction?.SortedSetRemoveAsync(key, userId);
                transaction?.SortedSetAddAsync(key, userId, _redisService.CalculateStringScore(newName));
            }
        }

        public List<IUser> GetGroupMembers(ushort groupId)
        {
            var database = _redisService.GetDatabase();
            var groupMembersKey = GroupMembersKey(groupId);
            RedisValue[] redisValues = database.SortedSetRangeByScore(groupMembersKey);
            var list = new List<IUser>();

            if(redisValues == null)
            {
                return list;
            }

            foreach(uint userId in redisValues)
            {
                list.Add(_usersService.Get(userId));
            }
            return list;
        }

        public IEnumerable<uint> GetGroupMemberIds(ushort groupId)
        {
            var database = _redisService.GetDatabase();
            var groupMembersKey = GroupMembersKey(groupId);
            var redisValues = database.SortedSetRangeByScore(groupMembersKey);
            if(redisValues == null)
            {
                yield break;
            }
            foreach(uint userId in redisValues)
            {
                yield return userId;
            }
        }

        public List<IGroup> GetUsersGroups(uint userId)
        {
            var database = _redisService.GetDatabase();
            var usersGroupsKey = UsersGroupsKey(userId);
            var redisValues = database.SortedSetRangeByScore(usersGroupsKey);
            var list = new List<IGroup>();
            if(redisValues == null)
            {
                return list;
            }
            foreach(var groupRedisValue in redisValues)
            {
                ushort groupId = (ushort)(uint)groupRedisValue;
                list.Add(_groupsService.Get(groupId));
            }
            return list;
        }

        public IEnumerable<uint> GetUsersGroupIds(uint userId)
        {
            var database = _redisService.GetDatabase();
            var usersGroupsKey = UsersGroupsKey(userId);
            var redisValues = database.SortedSetRangeByScore(usersGroupsKey);
            if(redisValues == null)
            {
                yield break;
            }
            foreach(var groupRedisValue in redisValues)
            {
                yield return (ushort)(uint)groupRedisValue;
            }
        }

        void ChangeGroupName(ushort groupId, string newName)
        {
            var db = _redisService.CurrentDatabase;
            if(db == null) throw new InvalidOperationException("No Current Database");
            var transaction = _redisService.CurrentTransaction;
            if(transaction == null) throw new InvalidOperationException("No Current Transaction");
            //find what users have this group
            foreach(int userId in db.SortedSetRangeByScore(GroupMembersKey(groupId)))
            {
                var key = UsersGroupsKey((uint)userId);
                transaction.SortedSetRemoveAsync(key, (int)groupId);
                transaction.SortedSetAddAsync(key, (int)groupId, _redisService.CalculateStringScore(newName));
            }
        }
    }
}