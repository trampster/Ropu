using Ropu.Gui.Shared.ViewModels;
using RopuForms.ViewModels;
using RopuForms.Views.TouchTracking;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RopuForms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PttPage : ContentPage
    {
        readonly PttViewModel<Color> _pttViewModel;
        public PttPage()
        {
            InitializeComponent();

            BindingContext = _pttViewModel = Inject.Injection.Resolve<PttViewModel<Color>>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _pttViewModel.Initialize();
        }

        void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            canvisView.OnTouchEffectAction(sender, args);
        }
    }
}