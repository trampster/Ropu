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

            var loadBalancerProtocol = new LoadBalancerProtocol(new PortFinder(), StartingControlPort);
            var serviceDiscovery = new ServiceDiscovery();
            var floorControl = new CallControl(loadBalancerProtocol, serviceDiscovery);

            await floorControl.Run();
        }
    }
}
