using System.Collections.Generic;

namespace Ropu.Shared.Groups
{
    public interface IGroup
    {
        ushort Id
        {
            get;
        }
        IEnumerable<uint> GroupMembers
        {
            get;
        }

        int MemberCount
        {
            get;
        }

        bool HasMember(uint userId);
    }
}