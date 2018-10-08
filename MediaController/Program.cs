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

            //this is requried to immediately throw if either task fails            
            var completedTask = await Task.WhenAny(callManagementTask, mediaTask, registerTask);
            await completedTask;//this will throw if the task complete with an error.

            //make sure both are complete before returning
            if(!callManagementTask.IsCompleted) await callManagementTask;
            if(!mediaTask.IsCompleted) await mediaTask;
            if(!registerTask.IsCompleted) await registerTask;
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
