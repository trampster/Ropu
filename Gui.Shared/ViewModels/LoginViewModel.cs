using System.Windows.Input;
using Ropu.Client;
using Ropu.Shared.Web;
using System.Threading.Tasks;
using System;
using Ropu.Gui.Shared.Services;

namespace Ropu.Gui.Shared.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        readonly IClientSettings _clientSettings;
        readonly INavigator _navigator;
        readonly RopuWebClient _webClient;
        readonly CredentialsProvider _credentialsProvider;
        
        public LoginViewModel(
            IClientSettings clientSettings, 
            INavigator navigator, 
            RopuWebClient webClient, 
            CredentialsProvider credentialProvider)
        {
            _clientSettings = clientSettings;
            _navigator = navigator;
            _webClient = webClient;
            _credentialsProvider = credentialProvider;
        }

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
            Console.WriteLine("Login");
            _credentialsProvider.Password = Password;
            _credentialsProvider.Email = Email;
            if(await _webClient.Login())
            {
                Console.WriteLine("Login Success");
                
                await _navigator.Show<PttViewModel>();
                return;
            }
            Console.WriteLine("Login Failure");
            FailureMessage = "Failed to login";
        });

        public ICommand Signup => new AsyncCommand(async () => 
        {
            await _navigator.ShowModal<SignupViewModel>();
            await Task.CompletedTask;
        });
    }
}
