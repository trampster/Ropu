using Eto.Drawing;
using Eto.Forms;
using Ropu.ClientUI.Services;
using Ropu.Gui.Shared.ViewModels;

namespace Ropu.ClientUI.Views
{
    public class SignupPage : Panel
    {
        readonly SignupViewModel _signupViewModel;
        readonly ImageService _imageService;

        public SignupPage(SignupViewModel signupViewModel, ImageService imageService)
        {
            _signupViewModel = signupViewModel;
            _imageService = imageService;
            DataContext = _signupViewModel;

            var emailBox = new TextBox();
            emailBox.TextBinding.BindDataContext<SignupViewModel>(m => m.Email);

            var nameBox = new TextBox();
            nameBox.TextBinding.BindDataContext<SignupViewModel>(m => m.Name);

            var passwordBox = new PasswordBox();
            passwordBox.TextBinding.BindDataContext<SignupViewModel>(m => m.Password);

            var retypePassordBox = new PasswordBox();
            retypePassordBox.TextBinding.BindDataContext<SignupViewModel>(m => m.RetypePassword);

            var signupButton = new Button(){Text = "Sign up"};
            signupButton.Command = _signupViewModel.Signup;

            var errorLabel = new Label()
            {
                TextColor = Color.FromArgb(0xFF,0,0,0xFF)
            };
            errorLabel.TextBinding.BindDataContext<SignupViewModel>(m => m.FailureMessage);

            //cancel;
            var backImageView = new ImageView(){Image = _imageService.Back};
            backImageView.MouseDown += (sender, args) => 
            {
                args.Handled = true;
            };
            backImageView.MouseUp += (sender, args) => _signupViewModel.Cancel.Execute(null);
            var cancelLabel = new Label() {Text = "Cancel"};
            cancelLabel.MouseDown += (sender, args) => args.Handled = true;
            cancelLabel.MouseUp += (sender, args) => _signupViewModel.Cancel.Execute(null);


            var cancelLayout = new StackLayout();
            cancelLayout.Padding = 10;
            cancelLayout.Orientation = Orientation.Horizontal;
            cancelLayout.Spacing = 5;
            cancelLayout.Items.Add(backImageView);
            cancelLayout.Items.Add(cancelLabel);

            // cancelLayout.MouseDown += (sender, args) => args.Handled = true;
            // cancelLayout.MouseUp += (sender, args) => _signupViewModel.Cancel.Execute(null);

            //sign in stuff
            var signinLayout = new DynamicLayout();
            signinLayout.BeginHorizontal();
                signinLayout.AddSpace();
                signinLayout.BeginVertical();
                    signinLayout.AddSpace();
                    signinLayout.Add(errorLabel);
                    signinLayout.AddSpace();
                    signinLayout.Add(new Label(){Text = "Email"});
                    signinLayout.Add(emailBox);
                    signinLayout.Add(new Label(){Text = "Name"});
                    signinLayout.Add(nameBox);
                    signinLayout.Add(new Label(){Text = "Password"});
                    signinLayout.Add(passwordBox);
                    signinLayout.Add(new Label(){Text = "Retype Password"});
                    signinLayout.Add(retypePassordBox);
                    signinLayout.AddSpace();
                    signinLayout.Add(signupButton);
                    signinLayout.AddSpace();
                    signinLayout.AddSpace();
                signinLayout.EndVertical();
                signinLayout.AddSpace();
            signinLayout.EndHorizontal();
            

            var pageLayout = new DynamicLayout();
            pageLayout.BeginVertical();
            pageLayout.Add(cancelLayout);
            pageLayout.Add(signinLayout, false, true);
            pageLayout.BeginVertical();
                
            Content = pageLayout;
        }
    }
}
