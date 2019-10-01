using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.Groups;
using Ropu.Shared;
using Ropu.Shared.Web;

namespace Ropu.LoadBalancer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Ropu Load Balancer");
            Console.WriteLine("Copyright (c) Daniel Hughes");
            Console.WriteLine();

            var settingsReader = new CommandLineSettingsReader();
            var settings = settingsReader.ParseArgs(args);
            if(settings == null)
            {
                return;
            }

            var credentialsProvider = new CredentialsProvider()
            {
                Email = settings.Email,
                Password = settings.Password
            };
            var webClient = new RopuWebClient("https://localhost:5001/", credentialsProvider);
            var groupsClient = new GroupsClient(webClient);
            var loadBalancerProtocol = new LoadBalancerProtocol(new PortFinder(), 5069);
            var controller = new LoadBalancerRunner(loadBalancerProtocol, groupsClient, webClient, settings);
            Console.WriteLine("Before Run");
            await controller.Run();
        }   
    }
}
