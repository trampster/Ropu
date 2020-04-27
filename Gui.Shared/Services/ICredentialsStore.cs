using System.Threading.Tasks;

namespace Ropu.Gui.Shared.Services
{
    public interface ICredentialsStore
    {
        Task Save(string email, string password);

        Task<(string email, string password)> Load();
    }
}
