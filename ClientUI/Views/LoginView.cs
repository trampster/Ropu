using Eto.Forms;
using Ropu.ClientUI.Services;
using Ropu.ClientUI.ViewModels;

namespace Ropu.ClientUI.Views
{
    public class LoginView : Panel
    {
        LoginViewModel _loginViewModel;

        public LoginView(LoginViewModel loginViewModel, ImageService imageService)
        {
            _loginViewModel = loginViewModel;
            DataContext = loginViewModel;

            var emailBox = new TextBox();
            emailBox.TextBinding.BindDataContext<LoginViewModel>(m => m.Email);

            var passwordBox = new PasswordBox();
            passwordBox.TextBinding.BindDataContext<LoginViewModel>(m => m.Password);

            var loginButton = new Button(){Text = "Login"};
            loginButton.Command = loginViewModel.Login;

            var signupButton = new LinkButton(){Text = "Sign up"};
            signupButton.Command = loginViewModel.Signup;

            var errorLabel = new Label(){};
            errorLabel.TextBinding.BindDataContext<LoginViewModel>(m => m.FailureMessage);

            var layout = new DynamicLayout();
            layout.BeginHorizontal();
                layout.AddSpace();
                layout.BeginVertical();
                    layout.AddSpace();
                    layout.Add(imageService.Ropu);
                    layout.Add(errorLabel);
                    layout.Add(new Label(){Text = "Email"});
                    layout.Add(emailBox);
                    layout.Add(new Label(){Text = "Password"});
                    layout.Add(passwordBox);
                    layout.AddSpace();
                    layout.Add(loginButton);
                    layout.AddSpace();
                    layout.Add(signupButton);
                    layout.AddSpace();
                layout.EndVertical();
                layout.AddSpace();
            layout.EndHorizontal();
            Content = layout;
        }
    }
}
