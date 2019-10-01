using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.ServingNode;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.Groups;
using System.Collections.Concurrent;
using Ropu.Shared.Web;

namespace Ropu.ServingNode
{
    class Program
    {
        const ushort StartingLoadBalancerPort = 5091;
        const ushort StartingServingNodePort = 5071;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Ropu Serving Node");
            Console.WriteLine("Copyright (c) Daniel Hughes");
            Console.WriteLine();

            var settingsReader = new CommandLineSettingsReader();
            var settings = settingsReader.ParseArgs(args);
            if(settings == null)
            {
                return;
            }

            var portFinder = new PortFinder();
            var mediaProtocol = new RopuProtocol(portFinder, StartingServingNodePort);
            var loadBalancerProtocol = new LoadBalancerProtocol(portFinder, StartingLoadBalancerPort);
            var serviceDiscovery = new ServiceDiscovery();
            var credentialsProvider = new CredentialsProvider()
            {
                Email = settings.Email,
                Password = settings.Password
            };
            var webClient = new RopuWebClient("https://localhost:5001", credentialsProvider); 
            var groupsClient = new GroupsClient(webClient);
            var registra = new Registra(groupsClient);
            var servingNodes = new ServingNodes(100);
            var groupCallControllerLookup = new GroupCallControllerLookup();

            var servingNodeRunner = new ServingNodeRunner(
                mediaProtocol, 
                loadBalancerProtocol, 
                serviceDiscovery,
                registra,
                servingNodes,
                groupCallControllerLookup);

            await servingNodeRunner.Run();
        }
    }
}
