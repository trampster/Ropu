using System.Collections.Generic;
using System.Net;

namespace Ropu.Shared.Groups
{
    public interface IGroupsClient
    {
        List<IPEndPoint> GetGroupMemberEndpoints(ushort groupId);
    }
}