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
        const ushort ControlPort = 5091;
        const ushort MediaPort = 5071;

        static async Task Main(string[] args)
        {
            var mediaProtocol = new MediaProtocol(MediaPort);
            var callManagementProtocol = new CallManagementProtocol(ControlPort);
            var serviceDiscovery = new ServiceDiscovery();
            var fileClient = new FileClient(callManagementProtocol);

            var mediaControllerRunner = new MediaControl(
                mediaProtocol, 
                callManagementProtocol, 
                serviceDiscovery,
                fileClient);

            await mediaControllerRunner.Run();
        }
    }
}
