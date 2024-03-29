﻿using Ropu.Client;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;
using Ropu.Client.FileAudio;
using Ropu.Shared.Groups;
using Ropu.Client.Opus;
using Ropu.Client.JitterBuffer;
using Ropu.ClientUI.Services;
using Ropu.Client.PulseAudio;
using Ropu.Shared.Web;
using Ropu.ClientUI.Views;
using Ropu.ClientUI.ViewModels;
using Ropu.Gui.Shared.ViewModels;
using Eto.Drawing;
using System;
using System.Threading.Tasks;
using Eto.Forms;

namespace Ropu.ClientUI
{
    public class Program
    {
        static void Main(string[] args)
        {
            const ushort controlPortStarting = 5061;

            var settingsManager = new CommandLineClientSettingsReader();

            if(!settingsManager.ParseArgs(args))
            {
                return;
            }

            var settings = settingsManager.ClientSettings;

            var webClient = new RopuWebClient("https://192.168.1.9:5001/", settingsManager);

            var keysClient = new KeysClient(webClient, false, encryptionKey => new CachedEncryptionKey(encryptionKey, key => new AesGcmWrapper(key)));
            var packetEncryption = new PacketEncryption(keysClient);

            var protocolSwitch = new ProtocolSwitch(controlPortStarting, new PortFinder(), packetEncryption, keysClient, settings);
            var servingNodeClient = new ServingNodeClient(protocolSwitch);

            IAudioSource audioSource = 
                settings.FileMediaSource != null ?
                (IAudioSource) new FileAudioSource(settings.FileMediaSource) :
                (IAudioSource) new PulseAudioSimple(StreamDirection.Record, "RopuInput");

            var audioPlayer = new PulseAudioSimple(StreamDirection.Playback, "RopuOutput");
            var audioCodec = new OpusCodec();
            var jitterBuffer = new AdaptiveJitterBuffer(2, 50);
            var mediaClient = new MediaClient(protocolSwitch, audioSource, audioPlayer, audioCodec, jitterBuffer, settings);
            var callManagementProtocol = new LoadBalancerProtocol(new PortFinder(), 5079, packetEncryption, keysClient);

            var beepPlayer = new BeepPlayer(new PulseAudioSimple(StreamDirection.Playback, "RopuBeeps"));
            

            var ropuClient = new RopuClient(protocolSwitch, servingNodeClient, mediaClient, callManagementProtocol, settings, beepPlayer, webClient, keysClient);

            var application = new RopuApplication(ropuClient);

            var imageService = new ImageService();

            //TODO: get web address from config
            var imageClient = new ImageClient(webClient);
            var groupsClient = new GroupsClient(webClient, imageClient);
            var usersClient = new UsersClient(webClient);
            //settings.UserId = usersClient.GetCurrentUser().Result.Id;
            var pttPage = new PttPage(imageService);

            var navigator = new Navigator();

            var colorService = new ColorService();
            
            navigator.Register<LoginViewModel, LoginView>(() => new LoginView(new LoginViewModel(navigator, webClient, settingsManager), imageService));
            
            navigator.Register<SignupViewModel, SignupPage>(() => new SignupPage(new SignupViewModel(navigator, usersClient), imageService));

            Action<Func<Task>> invoke = toDo => Application.Instance.Invoke(toDo);

            var permissionServices = new PermissionServices();

            var pttView = new PttView(new PttViewModel<Color>(ropuClient, settingsManager, groupsClient, usersClient, imageClient, colorService, invoke, permissionServices, webClient, navigator), pttPage);
            navigator.Register<PttViewModel<Color>, PttView>(() => pttView);
            navigator.RegisterView("HomeRightPanel", "PttView", () => pttView);


            var homeView = new HomeView(new HomeViewModel(navigator), navigator, colorService);

            navigator.Register<HomeViewModel, HomeView>(() => homeView);

            var browseGroupsView = new BrowseGroupsView(new BrowseGroupsViewModel(groupsClient, navigator));
            navigator.Register<BrowseGroupsViewModel, BrowseGroupsView>(() => browseGroupsView);

            Func<Group, BrowseGroupView> browseGroupViewBuilder = group => new BrowseGroupView(new BrowseGroupViewModel(group, groupsClient, settings, navigator), imageService, navigator, colorService);
            navigator.Register<BrowseGroupViewModel, BrowseGroupView, Group>(group => browseGroupViewBuilder(group));

            var selectIdleGroupView = new SelectIdleGroupView(new SelectGroupViewModel(groupsClient, navigator, ropuClient));
            navigator.RegisterView("HomeRightPanel", "SelectIdleGroupView", () => selectIdleGroupView);

            var mainForm = new MainView(navigator, new MainViewModel(settings, navigator));
            mainForm.Icon = imageService.Ropu;

            var ignore = navigator.ShowModal<HomeViewModel>();
            ignore = navigator.ShowPttView();
            application.Run(mainForm);
        }
    }
}
