using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Ropu.Shared.Groups
{
    public interface IGroupsClient
    {
        Task<IEnumerable<ushort>> GetGroups();

        Task<Group> Get(ushort groupId);

        Task<ushort[]> GetUsersGroups(uint userId);
    }
}