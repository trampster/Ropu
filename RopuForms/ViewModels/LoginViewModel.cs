﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Ropu.Client;
using Ropu.Shared.Web;
using RopuForms.Services;
using Xamarin.Forms;

namespace RopuForms.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        readonly IClientSettings _clientSettings;
        readonly INavigationService _navigator;
        readonly RopuWebClient _webClient;
        readonly CredentialsProvider _credentialsProvider;
        readonly ImageService _imageService;
        readonly ICredentialsStore _credentialsStore;

        public LoginViewModel(
            IClientSettings clientSettings,
            INavigationService navigator,
            RopuWebClient webClient,
            CredentialsProvider credentialProvider,
            ImageService imageService,
            ICredentialsStore credentialsService)
        {
            _clientSettings = clientSettings;
            _navigator = navigator;
            _webClient = webClient;
            _credentialsProvider = credentialProvider;
            _imageService = imageService;
            _credentialsStore = credentialsService;
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

        public ImageSource RopuIcon
        {
            get => _imageService.Ropu;
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
