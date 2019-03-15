using System.Collections.Generic;

namespace Ropu.Shared.Groups
{
    public class Group : IGroup
    {
        readonly HashSet<uint> _groupMembers;
        readonly ushort _id;

        public Group(ushort id)
        {
            _id = id;
            _groupMembers = new HashSet<uint>();
        }

        public string Name
        {
            get;
            set;
        }

        public void Add(uint unitId)
        {
            _groupMembers.Add(unitId);
        }

        public bool HasMember(uint userId)
        {
            return _groupMembers.Contains(userId);
        }

        public IEnumerable<uint> GroupMembers
        {
            get
            {
                return _groupMembers;
            }
        }

        public ushort Id => _id;

        public int MemberCount => _groupMembers.Count;

        public byte[] Image
        {
            get;
            set;
        }
    }
}