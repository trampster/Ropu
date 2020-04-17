using Eto.Forms;
using Ropu.ClientUI.ViewModels;

namespace Ropu.ClientUI.Views
{
    public class SignupPage : Panel
    {
        readonly SignupViewModel _signupViewModel;

        public SignupPage(SignupViewModel signupViewModel)
        {
            _signupViewModel = signupViewModel;
            DataContext = _signupViewModel;

            var emailBox = new TextBox();
            emailBox.TextBinding.BindDataContext<SignupViewModel>(m => m.Email);

            var nameBox = new TextBox();
            emailBox.TextBinding.BindDataContext<SignupViewModel>(m => m.Name);

            var passwordBox = new PasswordBox();
            passwordBox.TextBinding.BindDataContext<SignupViewModel>(m => m.Password);

            var retypePassordBox = new PasswordBox();
            retypePassordBox.TextBinding.BindDataContext<SignupViewModel>(m => m.RetypePassword);

            var signupButton = new Button(){Text = "Signup"};
            signupButton.Command = _signupViewModel.Signup;

            var errorLabel = new Label(){};
            errorLabel.TextBinding.BindDataContext<SignupViewModel>(m => m.FailureMessage);

            var layout = new DynamicLayout();
            layout.BeginHorizontal();
                layout.AddSpace();
                layout.BeginVertical();
                    layout.AddSpace();
                    layout.Add(errorLabel);
                    layout.Add(new Label(){Text = "Email"});
                    layout.Add(emailBox);
                    layout.Add(new Label(){Text = "Name"});
                    layout.Add(nameBox);
                    layout.Add(new Label(){Text = "Password"});
                    layout.Add(passwordBox);
                    layout.Add(new Label(){Text = "Retype Password"});
                    layout.Add(retypePassordBox);
                    layout.AddSpace();
                    layout.Add(signupButton);
                    layout.AddSpace();
                    layout.AddSpace();
                layout.EndVertical();
                layout.AddSpace();
            layout.EndHorizontal();
            Content = layout;
        }
    }
}
