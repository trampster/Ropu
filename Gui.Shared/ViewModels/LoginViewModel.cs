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
        readonly ISettingsManager _settingsManager;

        public LoginViewModel(
            IClientSettings clientSettings, 
            INavigator navigator, 
            RopuWebClient webClient, 
            ISettingsManager settingsManager)
        {
            _clientSettings = clientSettings;
            _navigator = navigator;
            _webClient = webClient;
            _settingsManager = settingsManager;
        }

        public override async Task Initialize()
        {
            var settings = _settingsManager.ClientSettings;
            Email = settings.Email ?? "";
            Password = settings.Password ?? "";
            await Task.CompletedTask;
        }

        public string ServerAddress
        {
            get => _clientSettings.WebAddress ?? "";
            set => _clientSettings.WebAddress = value;
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
            var settings = _settingsManager.ClientSettings;
            settings.Password = Password;
            settings.Email = Email;

            if(_clientSettings.WebAddress == null)
            {
                FailureMessage = "Missing server address.";
                return;
            }

            _webClient.ServerAddress = _clientSettings.WebAddress;
            try
            {
                if (await _webClient.Login())
                {
                    await _navigator.Back();

                    await _settingsManager.SaveSettings();
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
