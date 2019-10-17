using System;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared;
using System.Threading.Tasks;
using Ropu.Shared.Web;
using Ropu.Shared.WebModels;

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

            var settingsReader = new CommandLineSettingsReader();
            var settings = settingsReader.ParseArgs(args);
            if(settings == null)
            {
                return;
            }

            var portFinder = new PortFinder();
            var ropuProtocol = new RopuProtocol(portFinder, 9000);
            var loadBalancerProtocol = new LoadBalancerProtocol(portFinder, StartingControlPort);
            var serviceDiscovery = new ServiceDiscovery();
            var servingNodes = new ServingNodes(100);

            var credentialsProvider = new CredentialsProvider()
            {
                Email = settings.Email,
                Password = settings.Password
            };
            var webClient = new RopuWebClient("https://localhost:5001", credentialsProvider); 
            var servicesClient = new ServicesClient(webClient, ServiceType.CallController);

            var callControl = new CallControl(
                loadBalancerProtocol, 
                serviceDiscovery, 
                ropuProtocol, 
                servingNodes,
                servicesClient);
            
            await callControl.Run();
        }
    }
}
