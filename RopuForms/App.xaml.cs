using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using RopuForms.Services;
using RopuForms.Views;
using RopuForms.Inject;
using RopuForms.ViewModels;
using Ropu.Client;
using Ropu.Shared.Groups;
using Ropu.Shared;
using Ropu.Shared.WebModels;
using Ropu.Client.JitterBuffer;
using Ropu.Shared.Web;
using Ropu.Shared.OpenSsl;
using Client.NoAudio;
using Ropu.Client.Opus;
using Ropu.Shared.LoadBalancing;
using Ropu.Gui.Shared.ViewModels;
using Ropu.Gui.Shared.Services;

namespace RopuForms
{
    public partial class App : Application
    {

        public App()
        {
            Console.WriteLine("RegisterTypes (Xamarin.Forms)");
            Injection.RegisterTypes(RegisterTypes);

            InitializeComponent();


            var navigationService = Injection.Resolve<INavigationService>();
            Console.WriteLine("Register LoginPage with navigation service");

            navigationService.Register<LoginViewModel, LoginPage>(() => Injection.Resolve<LoginPage>());
            navigationService.Register<SignupViewModel, SignupPage>(() => Injection.Resolve<SignupPage>());
            navigationService.Register<BrowseGroupViewModel, BrowseGroupPage, Group>(group => Injection.Resolve<Func<Group, BrowseGroupPage>>()(group));

            var mainPage = Injection.Resolve<MainPage>();

            navigationService.AddRootPage(mainPage);

            MainPage = mainPage;
        }

        void RegisterTypes(Injection injection)
        {
            const ushort ControlPortStarting = 5061;

            injection
                .RegisterSingleton<IClientSettings>(i => new FormsClientSettings())
                .RegisterSingleton(i => new CredentialsProvider())
                .RegisterSingleton(i => new UsersClient(i.Get<RopuWebClient>()))
                .RegisterSingleton(i => new RopuWebClient("https://192.168.1.7:5001/", i.Get<CredentialsProvider>()))
                .RegisterSingleton<INavigationService>(i => new Navigator())
                .RegisterSingleton<INavigator>(i => i.Get<INavigationService>())
                .RegisterSingleton<ICredentialsStore>(i => new CredentialsStore())
                .RegisterSingleton(i => new LoginViewModel(i.Get<IClientSettings>(), i.Get<INavigationService>(), i.Get<RopuWebClient>(), i.Get<CredentialsProvider>(), i.Get<ICredentialsStore>()))
                .RegisterSingleton(i => new LoginPage(i.Get<LoginViewModel>()))
                .RegisterSingleton(i => new SignupViewModel(i.Get<INavigator>(), i.Get<UsersClient>()))
                .RegisterSingleton(i => new SignupPage(i.Get<SignupViewModel>()))
                .RegisterSingleton(i => new MainViewModel(i.Get<IClientSettings>(), i.Get<INavigationService>()))
                .RegisterSingleton(i => new BrowseGroupsViewModel(i.Get<IGroupsClient>(), i.Get<INavigationService>()))
                .RegisterSingleton(i => new BrowseGroupsPage(i.Get<BrowseGroupsViewModel>()))
                .RegisterSingleton(i => new MainPage(i.Get<MainViewModel>(), () => i.Get<BrowseGroupsPage>()))
                .RegisterSingleton(i => new ImageClient(i.Get<RopuWebClient>()))
                .RegisterSingleton<IGroupsClient>(i => new GroupsClient(i.Get<RopuWebClient>(), i.Get<ImageClient>()))
                .RegisterSingleton<Func<byte[], IAesGcm>>(i => key => new AesGcmOpenSslWrapper(key))
                .RegisterSingleton<Func<EncryptionKey, CachedEncryptionKey>>(i => encryptionKey => new CachedEncryptionKey(encryptionKey, i.Get<Func<byte[], IAesGcm>>()))
                .RegisterSingleton(i => new KeysClient(i.Get<RopuWebClient>(), false, i.Get<Func<EncryptionKey, CachedEncryptionKey>>()))
                .RegisterSingleton(i => new PacketEncryption(i.Get<KeysClient>()))
                .RegisterSingleton(i => new ProtocolSwitch(ControlPortStarting, i.Get<IPortFinder>(), i.Get<PacketEncryption>(), i.Get<KeysClient>(), i.Get<IClientSettings>()))
                .RegisterSingleton(i => new ServingNodeClient(i.Get<ProtocolSwitch>()))
                .RegisterSingleton<IJitterBuffer>(i => new AdaptiveJitterBuffer(2, 50))
                .RegisterSingleton<IAudioCodec>(i => new OpusCodec())
                .RegisterSingleton<IPortFinder>(i => new MobilePortFinder())
                .RegisterSingleton<IMediaClient>(i => new MediaClient(
                    i.Get<ProtocolSwitch>(), i.Get<IAudioSource>(), i.Get<IAudioPlayer>(), i.Get<IAudioCodec>(), i.Get<IJitterBuffer>(), i.Get<IClientSettings>()))
                .RegisterSingleton(i => new LoadBalancerProtocol(i.Get<IPortFinder>(), 5079, i.Get<PacketEncryption>(), i.Get<KeysClient>()))
                .RegisterSingleton<IBeepPlayer>(i => new NoBeepPlayer())
                .RegisterSingleton(i => new RopuClient(
                    i.Get<ProtocolSwitch>(), i.Get<ServingNodeClient>(), i.Get<IMediaClient>(), i.Get<LoadBalancerProtocol>(),
                    i.Get<IClientSettings>(), i.Get<IBeepPlayer>(), i.Get<RopuWebClient>(), i.Get<KeysClient>()))
                .Register<IUsersClient>(i => new UsersClient(i.Get<RopuWebClient>()))
                .Register<IPermissionService>(i => new PermissionService())
                .Register<IColorService<Color>>( i => new ColorService())
                .RegisterSingleton(i => new PttViewModel<Color>(
                    i.Get<RopuClient>(), 
                    i.Get<IClientSettings>(), 
                    i.Get<IGroupsClient>(), 
                    i.Get<IUsersClient>(), 
                    i.Get<ImageClient>(), 
                    i.Get<IColorService<Color>>(), 
                    toDo => Device.BeginInvokeOnMainThread(async () => await toDo()), 
                    i.Get<IPermissionService>(),
                    i.Get<RopuWebClient>()))
                .RegisterSingleton(i => new ImageService())
                .Register(i => new PttPage())
                .Register<Func<Group, BrowseGroupPage>>(i => group => new BrowseGroupPage(new BrowseGroupViewModel(group, i.Get<IGroupsClient>(), i.Get<IClientSettings>(), i.Get<INavigator>())));
        }

        protected override async void OnStart()
        {
            //var navigationService = Injection.Get<INavigationService>();
            //await navigationService.ShowModal<LoginViewModel>();
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
