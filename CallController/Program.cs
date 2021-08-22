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

             var credentialsProvider = new CredentialsProvider()
            {
                Email = settings.Email,
                Password = settings.Password
            };
            var webClient = new RopuWebClient("https://192.168.1.8:5001", credentialsProvider); 

            var portFinder = new PortFinder();
            var keysClient = new KeysClient(webClient, true, encryptionKey => new CachedEncryptionKey(encryptionKey, key => new AesGcmWrapper(key)));
            var packetEncryption = new PacketEncryption(keysClient);
            var ropuProtocol = new RopuProtocol(portFinder, 9000, packetEncryption);
            var loadBalancerProtocol = new LoadBalancerProtocol(portFinder, StartingControlPort, packetEncryption, keysClient);
            var serviceDiscovery = new ServiceDiscovery();
            var servingNodes = new ServingNodes(100);


            var servicesClient = new ServicesClient(webClient, ServiceType.CallController);

            var callControl = new CallControl(
                loadBalancerProtocol, 
                serviceDiscovery, 
                ropuProtocol, 
                servingNodes,
                servicesClient,
                keysClient);
            
            await callControl.Run();
        }
    }
}
