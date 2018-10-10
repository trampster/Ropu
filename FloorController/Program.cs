using System;
using Ropu.Shared.CallManagement;
using Ropu.Shared;
using System.Threading.Tasks;

namespace Ropu.FloorController
{
    class Program
    {
        const ushort ControlPort = 5080;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Ropu Floor Controller");

            var callManagementProtocol = new CallManagementProtocol(ControlPort);
            var serviceDiscovery = new ServiceDiscovery();
            var floorControl = new FloorControl(callManagementProtocol, serviceDiscovery);

            await floorControl.Run();
        }
    }
}
