using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.Client.Alsa;
using Ropu.Client.JitterBuffer;
using Ropu.Shared;
using Ropu.Shared.Groups;
using Ropu.Shared.LoadBalancing;

namespace Ropu.Client
{
    public class Program
    {
        const ushort _controlPortStarting = 5061;
        RopuClient _ropuClient;
        const string LoadBalancerIP =  "192.168.1.6";
        const string MyAddress = "192.168.1.6";
        const int LoadBalancerPort = 5069;
        IMediaClient _mediaClient;
        static void Main(string[] args)
        {
            var program = new Program();
            program.Run(args);
        }

        public void Run(string[] args)
        {
            var settings = new CommandLineClientSettings();
            if(!settings.ParseArgs(args))
            {
                return;
            }

            var protocolSwitch = new ProtocolSwitch(_controlPortStarting, new PortFinder());
            var servingNodeClient = new ServingNodeClient(protocolSwitch);

            var callManagementProtocol = new LoadBalancerProtocol(new PortFinder(), 5079);
            
            _mediaClient = BuildMediaClient(protocolSwitch, settings);
            var ipAddress = IPAddress.Parse(MyAddress);

            IPEndPoint loadBalancerEndpoint = new IPEndPoint(IPAddress.Parse(LoadBalancerIP), LoadBalancerPort);
            var beepPlayer = new BeepPlayer(new AlsaAudioPlayer());
            _ropuClient = new RopuClient(protocolSwitch, servingNodeClient, _mediaClient, ipAddress, callManagementProtocol, loadBalancerEndpoint, settings, beepPlayer);
            var ropuClientTask = _ropuClient.Run();

            //var consoleTask = TaskCordinator.RunLong(HandleCommands);
            
            //TaskCordinator.WaitAll(ropuClientTask, consoleTask).Wait();
            ropuClientTask.Wait();
        }

        IMediaClient BuildMediaClient(ProtocolSwitch protocolSwitch, IClientSettings settings)
        {
            if(settings.FakeMedia)
            {
                //Console.WriteLine("Using FakeMediaClient");
                return new FakeMediaClient();
            }
            var audioSource = new AlsaAudioSource();
            var audioPlayer = new AlsaAudioPlayer();
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
                    _ropuClient.StartCall(group);
                    break;
                default:
                    Console.WriteLine("I'm not sure what you mean.");
                    break;
            }
        }
    }
}
