using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.MediaController;
using Ropu.Shared;
using Ropu.Shared.CallManagement;

namespace ropu
{
    class Program
    {
        const ushort ControlPort = 5070;
        const ushort MediaPort = 5071;


        static async Task Main(string[] args)
        {
            var mediaProtocol = new MediaProtocol();
            var callManagementProtocol = new CallManagementProtocol(ControlPort);

            Task callManagementTask = callManagementProtocol.Run();
            Task mediaTask = mediaProtocol.Run();

            var registerTask = Register(callManagementProtocol);

            await TaskCordinator.WaitAll(callManagementTask, mediaTask, registerTask);
        }

        public static async Task Register(CallManagementProtocol callManagement)
        {
            while(true)
            {
                var serviceDiscovery = new ServiceDiscovery();
                var callManagementServerEndpoint = serviceDiscovery.CallManagementServerEndpoint();
                Console.WriteLine(callManagementServerEndpoint);
                bool registered = await callManagement.RegisterMediaController(
                    ControlPort, 
                    new IPEndPoint(serviceDiscovery.GetMyAddress(), MediaPort), 
                    callManagementServerEndpoint);
                if(registered)
                {
                    Console.WriteLine("Registered");
                    await Task.Delay(60000);
                    continue;
                }
                Console.WriteLine("Failed to register");
            }
        }
    }
}
