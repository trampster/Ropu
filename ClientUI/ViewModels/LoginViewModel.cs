using System.Windows.Input;
using Ropu.Client;
using Ropu.Shared.Web;
using System.Threading.Tasks;
using Ropu.ClientUI.Views;
using System;

namespace Ropu.ClientUI.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        readonly IClientSettings _clientSettings;
        readonly Navigator _navigator;
        readonly RopuWebClient _webClient;
        readonly CredentialsProvider _credentialsProvider;
        
        public LoginViewModel(
            IClientSettings clientSettings, 
            Navigator navigator, 
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
                
                _navigator.Show<PttView>();
                return;
            }
            Console.WriteLine("Login Failure");
            FailureMessage = "Failed to login";
        });
    }
}
