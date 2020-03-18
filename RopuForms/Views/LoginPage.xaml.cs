using RopuForms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RopuForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginViewModel loginViewModel)
        {
            InitializeComponent();
            BindingContext = loginViewModel;
        }
    }
}