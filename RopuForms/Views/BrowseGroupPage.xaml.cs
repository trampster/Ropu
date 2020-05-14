using Ropu.Gui.Shared.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RopuForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BrowseGroupPage : ContentPage
    {
        readonly BrowseGroupViewModel _browseGroupViewModel;

        public BrowseGroupPage(BrowseGroupViewModel browseGroupViewModel)
        {
            InitializeComponent();
            _browseGroupViewModel = browseGroupViewModel;
            BindingContext = _browseGroupViewModel;
        }
    }
}