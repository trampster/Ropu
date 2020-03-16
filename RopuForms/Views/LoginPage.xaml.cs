using RopuForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RopuForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        readonly LoginViewModel _loginViewModel;

        //public LoginPage()
        //{
        //    InitializeComponent();
        //    _loginViewModel = null;
        //}

        public LoginPage(LoginViewModel loginViewModel)
        {
            InitializeComponent();
            _loginViewModel = loginViewModel;
            BindingContext = _loginViewModel;
        }
    }
}