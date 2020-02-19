using Ropu.Client;
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

namespace Ropu.ClientUI
{
    public class Program
    {
        static void Main(string[] args)
        {
            const ushort controlPortStarting = 5061;

            var settingsReader = new CommandLineClientSettingsReader();

            var settings = settingsReader.ParseArgs(args);
            if(settings == null)
            {
                return;
            }

            var credentialsProvider = new CredentialsProvider();
            var webClient = new RopuWebClient("https://localhost:5001/", credentialsProvider);

            var keysClient = new KeysClient(webClient, false);
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

            var beepPlayer = new BeepPlayer(new PulseAudioSimple(StreamDirection.Record, "RopuBeeps"));
            

            var ropuClient = new RopuClient(protocolSwitch, servingNodeClient, mediaClient, callManagementProtocol, settings, beepPlayer, webClient, keysClient);

            var application = new RopuApplication(ropuClient);

            var imageService = new ImageService();

            //TODO: get web address from config
            var groupsClient = new GroupsClient(webClient);
            var usersClient = new UsersClient(webClient);
            //settings.UserId = usersClient.GetCurrentUser().Result.Id;
            var imageClient = new ImageClient(webClient);
            var pttPage = new PttPage(imageService);

            var navigator = new Navigator();
            navigator.Register(() => new LoginView(new LoginViewModel(settings, navigator, webClient, credentialsProvider), imageService));

            var pttView = new PttView(new PttViewModel(ropuClient, settings, groupsClient, usersClient, imageClient), pttPage);
            navigator.Register(() => pttView);

            var mainForm = new MainView(navigator, new MainViewModel(settings, navigator));
            mainForm.Icon = imageService.Ropu;
            application.Run(mainForm);
        }
    }
}
