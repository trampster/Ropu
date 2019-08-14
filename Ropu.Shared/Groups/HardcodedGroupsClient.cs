using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Ropu.Shared.Groups
{
    public class HardcodedGroupsClient : IGroupsClient
    {
        readonly Dictionary<ushort, IGroup> _groupLookup;
        readonly string _imageFolder;

        public HardcodedGroupsClient(string imageFolder)
        {
            _imageFolder = imageFolder;
            _groupLookup = new Dictionary<ushort, IGroup>();
            AddTestGroup(4242, "Avengers", 1000, 2000);
            AddTestGroup(1234, "Justice L", 2000, 3000);
        }

        public IEnumerable<IGroup> Groups
        {
            get
            {
                return _groupLookup.Values;
            }
        }

        public int GroupCount => _groupLookup.Count;

        public IGroup Get(ushort groupId)
        {
            if(_groupLookup.TryGetValue(groupId, out IGroup group))
            {
                return group;
            }
            return null;
        }

        public IEnumerable<ushort> GetUsersGroups(uint userId)
        {
            foreach(var group in _groupLookup)
            {
                if(group.Value.HasMember(userId))

                {
                    yield return group.Key;
                }
            }
        }

        void AddTestGroup(ushort groupId, string name, uint startUnitId, uint endUnitId)
        {
            var group = new Group(groupId);
            group.Name = name;

            for(uint unitId = startUnitId; unitId <= endUnitId; unitId++)
            {
                group.Add(unitId);
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var iconPath = Path.Combine(home, "RopuIcons", $"{name}.png");
            if(File.Exists(iconPath))
            {
                group.Image = File.ReadAllBytes(iconPath);
            }
            else
            {
                Console.WriteLine($"Failed to find group icon at {iconPath} using default icon instead.");
                group.Image = File.ReadAllBytes(Path.Combine(_imageFolder, "knot32.png"));
            }
            _groupLookup.Add(groupId, group);

        }
        
    }
}