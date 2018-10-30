using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.ServingNode;
using Ropu.Shared;
using Ropu.Shared.CallManagement;
using Ropu.Shared.Registra;

namespace Ropu.ServingNode
{
    class Program
    {
        const ushort ControlPort = 5091;
        const ushort MediaPort = 5071;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Ropu Serving Node");
            Console.WriteLine("Copyright (c) Daniel Hughes");
            var mediaProtocol = new MediaProtocol(MediaPort);
            var callManagementProtocol = new CallManagementProtocol(ControlPort);
            var serviceDiscovery = new ServiceDiscovery();
            var fileClient = new FileClient(callManagementProtocol);
            var registraClient = new RegistraClient(serviceDiscovery, callManagementProtocol, fileClient);

            var servingNodeRunner = new ServingNodeRunner(
                mediaProtocol, 
                callManagementProtocol, 
                serviceDiscovery,
                registraClient);

            await servingNodeRunner.Run();
        }
    }
}
