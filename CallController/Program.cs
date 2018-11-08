using System;
using Ropu.Shared.CallManagement;
using Ropu.Shared;
using System.Threading.Tasks;

namespace Ropu.CallController
{
    class Program
    {
        const ushort ControlPort = 5080;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Ropu Call Controller");
            Console.WriteLine("Copyright (c) Daniel Hughes 2018");

            var loadBalancerProtocol = new LoadBalancerProtocol(ControlPort);
            var serviceDiscovery = new ServiceDiscovery();
            var floorControl = new CallControl(loadBalancerProtocol, serviceDiscovery);

            await floorControl.Run();
        }
    }
}
