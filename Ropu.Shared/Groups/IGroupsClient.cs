using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Ropu.Shared.Groups
{
    public interface IGroupsClient
    {
        Task<IEnumerable<ushort>> GetGroups();

        Task<ushort[]> GetMyGroups(uint myUserId);

        Task<Group?> Get(ushort groupId);

        Task<ushort[]> GetUsersGroups(uint userId);

        Task<bool> Leave(ushort groupId, uint userId);

        Task<bool> Join(ushort groupId, uint userId);
    }
}