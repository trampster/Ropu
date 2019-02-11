using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;

namespace Ropu.Client
{
    class Program
    {
        const ushort _controlPortStarting = 5061;
        static RopuClient _ropuClient;
        const string LoadBalancerIP =  "192.168.1.6";
        const string MyAddress = "192.168.1.6";
        const int LoadBalancerPort = 5069;
        static MediaClient _mediaClient;
        static async Task Main(string[] args)
        {
            var settings = new CommandLineClientSettings();
            if(!settings.ParseArgs(args))
            {
                return;
            }

            var protocolSwitch = new ProtocolSwitch(_controlPortStarting, new PortFinder());
            var servingNodeClient = new ServingNodeClient(protocolSwitch);
            var audioSource = new AlsaAudioSource();
            _mediaClient = new MediaClient(protocolSwitch, audioSource, settings);
            var callManagementProtocol = new LoadBalancerProtocol(new PortFinder(), 5079);

            var ipAddress = IPAddress.Parse(MyAddress);

            IPEndPoint loadBalancerEndpoint = new IPEndPoint(IPAddress.Parse(LoadBalancerIP), LoadBalancerPort);
            _ropuClient = new RopuClient(protocolSwitch, servingNodeClient, ipAddress, callManagementProtocol, loadBalancerEndpoint, settings);
            var ropuClientTask = _ropuClient.Run();

            var consoleTask = TaskCordinator.RunLong(HandleCommands);
            
            await TaskCordinator.WaitAll(ropuClientTask, consoleTask);
        }

        static void HandleCommands()
        {
            Console.Write(">");

            while(true)
            {
                var command = Console.ReadLine();
                HandleCommand(command);
                Console.Write(">");
            }
        }

        static void HandleCommand(string commandLine)
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
