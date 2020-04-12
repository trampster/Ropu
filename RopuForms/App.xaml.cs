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

            navigationService.Register<LoginPage>(() => Injection.Resolve<LoginPage>());

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
                .RegisterSingleton(i => new RopuWebClient("https://192.168.1.7:5001/", i.Get<CredentialsProvider>()))
                .RegisterSingleton<INavigationService>(i => new Navigator())
                .RegisterSingleton<ICredentialsStore>(i => new CredentialsStore())
                .RegisterSingleton(i => new LoginViewModel(i.Get<IClientSettings>(), i.Get<INavigationService>(), i.Get<RopuWebClient>(), i.Get<CredentialsProvider>(), i.Get<ImageService>(), i.Get<ICredentialsStore>()))
                .RegisterSingleton(i => new LoginPage(i.Get<LoginViewModel>()))
                .RegisterSingleton(i => new MainViewModel(i.Get<IClientSettings>(), i.Get<INavigationService>()))
                .RegisterSingleton(i => new MainPage(i.Get<MainViewModel>()))
                .RegisterSingleton(i => new ImageClient(i.Get<RopuWebClient>()))
                .RegisterSingleton<IGroupsClient>(i => new GroupsClient(i.Get<RopuWebClient>(), i.Get<ImageClient>()))
                .RegisterSingleton<Func<byte[], IAesGcm>>(i => key => new AesGcmOpenSsl(key))
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
                .RegisterSingleton(i => new PttViewModel(i.Get<RopuClient>(), i.Get<IClientSettings>(), i.Get<IGroupsClient>(), i.Get<IUsersClient>(), i.Get<ImageClient>(), i.Get<RopuWebClient>()))
                .RegisterSingleton(i => new ImageService())
                .Register(i => new PttPage());
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
