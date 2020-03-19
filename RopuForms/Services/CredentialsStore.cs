using System.Threading.Tasks;
using Xamarin.Essentials;

namespace RopuForms.Services
{
    public class CredentialsStore : ICredentialsStore
    {
        public async Task<(string email, string password)> Load()
        {
            string email = await SecureStorage.GetAsync("email");
            string password = await SecureStorage.GetAsync("password");
            return (email, password);
        }

        public async Task Save(string email, string password)
        {
            await SecureStorage.SetAsync("email", email);
            await SecureStorage.SetAsync("password", password);
        }
    }
}
