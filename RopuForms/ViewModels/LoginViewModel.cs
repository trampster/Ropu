using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using Ropu.Client;
using Ropu.Shared.Web;
using RopuForms.Services;

namespace RopuForms.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        readonly IClientSettings _clientSettings;
        readonly INavigationService _navigator;
        readonly RopuWebClient _webClient;
        readonly CredentialsProvider _credentialsProvider;

        public LoginViewModel(
            IClientSettings clientSettings,
            INavigationService navigator,
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
            try
            {
                if (await _webClient.Login())
                {
                    Console.WriteLine("Login Success");
                    await _navigator.Back();
                    return;
                }
                Console.WriteLine("Login Failure");
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
