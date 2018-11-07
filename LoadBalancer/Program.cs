using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.LoadBalancer.FileServer;
using Ropu.Shared.CallManagement;
using Ropu.Shared.Groups;

namespace Ropu.LoadBalancer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var fileManager = new FileManager();
            var groupsClient = new HardcodedGroupsClient();
            var registra = new Registra(groupsClient);
            var controlProtocol = new ControlProtocol(5060);
            var callManagementProtocol = new CallManagementProtocol(5069);
            var controller = new ControlFunction(controlProtocol, registra, callManagementProtocol, groupsClient, fileManager);
            await controller.Run();
        }   
    }
}
