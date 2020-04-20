using System.Windows.Input;
using Ropu.Shared.Web.Models;
using Ropu.Shared.Groups;
using Ropu.Gui.Shared.Services;

namespace Ropu.Gui.Shared.ViewModels
{
    public class SignupViewModel : BaseViewModel
    {
        readonly INavigator _navigator;
        readonly UsersClient _usersClient;
        
        public SignupViewModel(
            INavigator navigator, 
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
            if(!Email.Contains("@") || Email.Contains(' '))
            {
                FailureMessage = "Email is invalid";
                return;
            }
            if(Name == null || Name == "")
            {
                FailureMessage = "Name is a requried field";
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
                //_navigator.Show<LoginViewModel>();
                return;
            }
            FailureMessage = "Failed to signup";
        });
    }
}
