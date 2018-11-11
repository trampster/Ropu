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

            var portFinder = new PortFinder();
            var mediaProtocol = new MediaProtocol(portFinder, StartingServingNodePort);
            var loadBalancerProtocol = new LoadBalancerProtocol(portFinder, StartingLoadBalancerPort);
            var serviceDiscovery = new ServiceDiscovery();
            var groupsClient = new HardcodedGroupsClient();
            var registra = new Registra(groupsClient);
            var servingNodes = new ServingNodes();

            var servingNodeRunner = new ServingNodeRunner(
                mediaProtocol, 
                loadBalancerProtocol, 
                serviceDiscovery,
                registra,
                servingNodes);

            await servingNodeRunner.Run();
        }
    }
}
