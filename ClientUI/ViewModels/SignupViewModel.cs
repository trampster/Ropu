using System.Windows.Input;
using Ropu.Client;
using Ropu.Shared.Web;
using Ropu.ClientUI.Views;
using System;
using Ropu.Shared.Web.Models;
using Ropu.Shared.Groups;

namespace Ropu.ClientUI.ViewModels
{
    public class SignupViewModel : BaseViewModel
    {
        readonly Navigator _navigator;
        readonly UsersClient _usersClient;
        
        public SignupViewModel(
            Navigator navigator, 
            UsersClient usersClient)
        {
            _navigator = navigator;
            _usersClient = usersClient;
        }

        string _email = "";

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        string _name = "";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        string _password = "";

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        string _retypePassword = "";

        public string RetypePassword
        {
            get => _retypePassword;
            set => SetProperty(ref _retypePassword, value);
        }

        string _failureMessage = "";
        public string FailureMessage
        {
            get => _failureMessage;
            set => SetProperty(ref _failureMessage, value);
        }

        public ICommand Signup => new AsyncCommand(async () => 
        {
            if(Password != RetypePassword)
            {
                FailureMessage = "Passwords don't match";
                return;
            }
            var newUser = new NewUser()
            {
                Name = Name,
                Password = Password,
                Email = Email
            };
            if(await _usersClient.Create(newUser))
            {
                _navigator.Show<LoginView>();
                return;
            }
            FailureMessage = "Failed to login";
        });
    }
}
