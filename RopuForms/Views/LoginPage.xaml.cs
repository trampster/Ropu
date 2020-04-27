using Ropu.Gui.Shared.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RopuForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        readonly LoginViewModel _loginViewModel;
        public LoginPage(LoginViewModel loginViewModel)
        {
            InitializeComponent();
            _loginViewModel = loginViewModel;
            BindingContext = loginViewModel;
        }

        protected override async void OnAppearing()
        {
            await _loginViewModel.Initialize();
            base.OnAppearing();
        }
    }
}