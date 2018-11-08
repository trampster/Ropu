using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.LoadBalancer.FileServer;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.Groups;

namespace Ropu.LoadBalancer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Ropu Load Balancer");
            Console.WriteLine("Copyright (c) Daniel Hughes");

            var fileManager = new FileManager();
            var groupsClient = new HardcodedGroupsClient();
            var loadBalancerProtocol = new LoadBalancerProtocol(5069);
            var controller = new LoadBalancerRunner(loadBalancerProtocol, groupsClient, fileManager);
            await controller.Run();
        }   
    }
}
