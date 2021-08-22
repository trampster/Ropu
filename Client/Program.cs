using System;
using Ropu.Client.Alsa;
using Ropu.Client.JitterBuffer;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.Web;

namespace Ropu.Client
{
    public class Program
    {
        const ushort _controlPortStarting = 5061;
        RopuClient? _ropuClient;
        const int LoadBalancerPort = 5069;
        IMediaClient? _mediaClient;

        public void Run(string[] args)
        {
            var settingsReader = new CommandLineClientSettingsReader();
            var settings = settingsReader.ParseArgs(args);
            if(settings == null)
            {
                return;
            }

            var credentials = new CredentialsProvider()
            {
                Email = settings.Email,
                Password = settings.Password
            };

            var webClient = new RopuWebClient("asdf", credentials);


            var keysClient = new KeysClient(webClient, false, encryptionKey => new CachedEncryptionKey(encryptionKey, key => new AesGcmWrapper(key)));
            var packetEncryption = new PacketEncryption(keysClient);

            var protocolSwitch = new ProtocolSwitch(_controlPortStarting, new PortFinder(), packetEncryption, keysClient, settings);
            var servingNodeClient = new ServingNodeClient(protocolSwitch);

            var callManagementProtocol = new LoadBalancerProtocol(new PortFinder(), 5079, packetEncryption, keysClient);
            
            _mediaClient = BuildMediaClient(protocolSwitch, settings);

            //IPEndPoint loadBalancerEndpoint = new IPEndPoint(settings.LoadBalancerIPAddress, LoadBalancerPort);
            var beepPlayer = BuildBeepPlayer(settings);



            _ropuClient = new RopuClient(protocolSwitch, servingNodeClient, _mediaClient, callManagementProtocol, settings, beepPlayer, webClient, keysClient);
            var ropuClientTask = _ropuClient.Run();

            //var consoleTask = TaskCordinator.RunLong(HandleCommands);
            
            //TaskCordinator.WaitAll(ropuClientTask, consoleTask).Wait();
            ropuClientTask.Wait();
        }

        IBeepPlayer BuildBeepPlayer(IClientSettings settings)
        {
            if(settings.FakeMedia)
            {
                //Console.WriteLine("Using FakeMediaClient");
                return new SilentBeepPlayer();
            }
            return new BeepPlayer(new AlsaAudioPlayer(false));     
        }

        IMediaClient BuildMediaClient(ProtocolSwitch protocolSwitch, IClientSettings settings)
        {
            if(settings.FakeMedia)
            {
                //Console.WriteLine("Using FakeMediaClient");
                return new FakeMediaClient();
            }
            var audioSource = new AlsaAudioSource();
            var audioPlayer = new AlsaAudioPlayer(true);
            var audioCodec = new RawCodec();
            var jitterBuffer = new AdaptiveJitterBuffer(2, 50);

            return new MediaClient(protocolSwitch, audioSource, audioPlayer, audioCodec, jitterBuffer, settings);
        }

        void HandleCommands()
        {
            Console.Write(">");

            while(true)
            {
                var command = Console.ReadLine();
                HandleCommand(command);
                Console.Write(">");
            }
        }

        void HandleCommand(string commandLine)
        {
            if(string.IsNullOrEmpty(commandLine))
            {
                return;
            }
            char command = commandLine[0];

            switch(command)
            {
                case 'g':
                    if(commandLine.Length < 3)
                    {
                        Console.WriteLine("error: You must specify the group to call");
                        return;
                    }
                    if(!ushort.TryParse(commandLine.AsSpan(2), out ushort group))
                    {
                        Console.WriteLine("error: You must specify the group to call as a number");
                        return;
                    }
                    _ropuClient?.StartCall(group);
                    break;
                default:
                    Console.WriteLine("I'm not sure what you mean.");
                    break;
            }
        }
    }
}
