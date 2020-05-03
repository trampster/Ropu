using System.Windows.Input;
using Ropu.Client;
using Ropu.Shared.Web;
using System.Threading.Tasks;
using System;
using Ropu.Gui.Shared.Services;
using System.Net.Http;

namespace Ropu.Gui.Shared.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        readonly IClientSettings _clientSettings;
        readonly INavigator _navigator;
        readonly RopuWebClient _webClient;
        readonly CredentialsProvider _credentialsProvider;
        readonly ICredentialsStore _credentialsStore;

        public LoginViewModel(
            IClientSettings clientSettings, 
            INavigator navigator, 
            RopuWebClient webClient, 
            CredentialsProvider credentialProvider,
            ICredentialsStore credentialsStore)
        {
            _clientSettings = clientSettings;
            _navigator = navigator;
            _webClient = webClient;
            _credentialsProvider = credentialProvider;
            _credentialsStore = credentialsStore;
        }

        public override async Task Initialize()
        {
            (string email, string password) = await _credentialsStore.Load();
            Email = email;
            Password = password;
        }

        public string ServerAddress
        {
            get => _webClient.ServerAddress;
            set => _webClient.ServerAddress = value;
        }

        bool _isServerAddressEditable = false;
        public bool IsServerAddressEditable
        {
            get => _isServerAddressEditable;
            set => SetProperty(ref _isServerAddressEditable, value);
        }

        public ICommand ToggleEditServerAddress => new AsyncCommand(async () =>
        {
            IsServerAddressEditable = !IsServerAddressEditable;
            await Task.CompletedTask;
        });

        public ICommand Signup => new AsyncCommand(async () =>
        {
            await _navigator.ShowModal<SignupViewModel>();
        });

        string _email = "";
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        string _password = "";

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        string _failureMessage = "";
        public string FailureMessage
        {
            get => _failureMessage;
            set => SetProperty(ref _failureMessage, value);
        }

        public ICommand Login => new AsyncCommand(async () => 
        {
            _credentialsProvider.Password = Password;
            _credentialsProvider.Email = Email;
            try
            {
                if (await _webClient.Login())
                {
                    await _navigator.Back();
                
                    await _credentialsStore.Save(Email, Password);

                    return;
                }
                FailureMessage = "Failed to login";
            }
            catch(HttpRequestException exception)
            {
                Console.WriteLine($"Login Failure {exception}");
                FailureMessage = "Failed to talk to Ropu Server";
            }
        });
    }
}
