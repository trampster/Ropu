using System;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared;
using System.Threading.Tasks;

namespace Ropu.CallController
{
    class Program
    {
        const ushort StartingControlPort = 5080;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Ropu Call Controller");
            Console.WriteLine("Copyright (c) Daniel Hughes 2018");
            Console.WriteLine();

            var portFinder = new PortFinder();
            var ropuProtocol = new RopuProtocol(portFinder, 9000);
            var loadBalancerProtocol = new LoadBalancerProtocol(portFinder, StartingControlPort);
            var serviceDiscovery = new ServiceDiscovery();
            var servingNodes = new ServingNodes(100);
            var callControl = new CallControl(loadBalancerProtocol, serviceDiscovery, ropuProtocol, servingNodes);

            await callControl.Run();
        }
    }
}
