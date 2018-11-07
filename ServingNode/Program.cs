using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.ServingNode;
using Ropu.Shared;
using Ropu.Shared.CallManagement;
using Ropu.Shared.Groups;

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
            var groupsClient = new HardcodedGroupsClient();
            var registra = new Registra(groupsClient);

            var servingNodeRunner = new ServingNodeRunner(
                mediaProtocol, 
                callManagementProtocol, 
                serviceDiscovery,
                registra);

            await servingNodeRunner.Run();
        }
    }
}
