using System;
using System.Threading.Tasks;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.Groups;
using Ropu.Shared;
using Ropu.Shared.Web;
using Ropu.Shared.WebModels;

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
            var webClient = new RopuWebClient("https://192.168.1.8:5001/", credentialsProvider);
            var groupsClient = new GroupsClient(webClient, new ImageClient(webClient));
            

            var keysClient = new KeysClient(webClient, true, encryptionKey => new CachedEncryptionKey(encryptionKey, key => new AesGcmWrapper(key)));
            var packetEncryption = new PacketEncryption(keysClient);
            var loadBalancerProtocol = new LoadBalancerProtocol(new PortFinder(), 5069, packetEncryption, keysClient);
            
            var servicesClient = new ServicesClient(webClient, ServiceType.LoadBalancer);

            var controller = new LoadBalancerRunner(keysClient, loadBalancerProtocol, groupsClient, webClient, settings, servicesClient);
            await controller.Run();
        }   
    }
}
