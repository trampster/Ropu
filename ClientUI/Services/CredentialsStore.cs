using System.Threading.Tasks;
using Ropu.Gui.Shared.Services;

namespace Ropu.ClientUI.Services
{
    public class CredentialsStore : ICredentialsStore
    {
        public async Task<(string email, string password)> Load()
        {
            await Task.CompletedTask;
            return ("", "");
        }

        public async Task Save(string email, string password)
        {
            await Task.CompletedTask;
        }
    }
}