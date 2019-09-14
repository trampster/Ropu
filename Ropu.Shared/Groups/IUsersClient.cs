using System.Threading.Tasks;
using Ropu.Shared.WebModels;

namespace Ropu.Shared.Groups
{
    public interface IUsersClient
    {
        Task<IUser> Get(uint userId);
        Task<IUser> GetCurrentUser();
    }
}