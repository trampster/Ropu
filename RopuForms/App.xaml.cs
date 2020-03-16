using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using RopuForms.Services;
using RopuForms.Views;
using RopuForms.Inject;
using RopuForms.ViewModels;

namespace RopuForms
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            var navigationService = Injection.Resolve<INavigationService>();
            navigationService.Register<LoginPage>(() => Injection.Resolve<LoginPage>());

            var mainPage = Injection.Resolve<MainPage>();

            navigationService.AddRootPage(mainPage);

            MainPage = mainPage;
        }

        protected override async void OnStart()
        {
            //var navigationService = Injection.Get<INavigationService>();
            //await navigationService.ShowModal<LoginPage>();
            await Injection.Resolve<MainViewModel>().Initialize();
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
