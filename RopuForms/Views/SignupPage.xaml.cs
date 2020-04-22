using Ropu.Gui.Shared.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RopuForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SignupPage : ContentPage
    {
        readonly SignupViewModel _signupViewModel;
        public SignupPage(SignupViewModel signupViewModel)
        {
            _signupViewModel = signupViewModel;
            InitializeComponent();
            BindingContext = _signupViewModel;
        }
    }
}