using Ropu.Client;
using Ropu.Gui.Shared.Services;
using Ropu.Shared.Web;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace RopuForms.Services
{
    class XamarinSettingsManager : ISettingsManager, ICredentialsProvider
    {
        readonly ICredentialsStore _credentialsStore;
        IClientSettings _clientSettings;

        public XamarinSettingsManager(IClientSettings clientSettings, ICredentialsStore credentialsStore)
        {
            _credentialsStore = credentialsStore;
            _clientSettings = clientSettings;
        }

        public async Task Initialize()
        {
            (var email, var password) = await _credentialsStore.Load();
            string webAddress = await SecureStorage.GetAsync("webAddress");

            _clientSettings.Email = email;
            _clientSettings.Password = password;
            _clientSettings.WebAddress = webAddress;
        }

        public IClientSettings ClientSettings => _clientSettings;

        public string Email
        {
            get => _clientSettings.Email;
            set => _clientSettings.Email = value;
        }

        public string Password
        {
            get => _clientSettings.Password;
            set => _clientSettings.Password = value;
        }

        public async Task SaveSettings()
        {
            await _credentialsStore.Save(_clientSettings.Email, _clientSettings.Password);
            await SecureStorage.SetAsync("webAddress", _clientSettings.WebAddress);
        }
    }
}
