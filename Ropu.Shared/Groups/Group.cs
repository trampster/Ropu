using System.Collections.Generic;

namespace Ropu.Shared.Groups
{
    public class Group : IGroup
    {
        readonly List<uint> _groupMembers;
        readonly ushort _id;

        public Group(ushort id)
        {
            _id = id;
            _groupMembers = new List<uint>();
        }

        public void Add(uint unitId)
        {
            _groupMembers.Add(unitId);
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
    }
}