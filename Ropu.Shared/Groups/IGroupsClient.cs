using System.Collections.Generic;
using System.Net;

namespace Ropu.Shared.Groups
{
    public interface IGroupsClient
    {
        IEnumerable<IGroup> Groups
        {
            get;
        }

        IGroup Get(ushort groupId);

        int GroupCount
        {
            get;
        }

        IEnumerable<ushort> GetUsersGroups(uint userId);
    }
}