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

        string Name
        {
            get;
        }

        bool HasMember(uint userId);

        byte[] Image
        {
            get;
        }
    }
}