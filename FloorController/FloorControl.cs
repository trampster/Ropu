using System.Threading.Tasks;
using Ropu.Shared.CallManagement;
using Ropu.Shared;
using System.Net;
using System;

namespace Ropu.FloorController
{
    public class FloorControl
    {
        readonly CallManagementProtocol _callManagementProtocol;

        readonly ServiceDiscovery _serviceDiscovery;

        public FloorControl(CallManagementProtocol callManagementProtocol, ServiceDiscovery serviceDiscovery)
        {
            _callManagementProtocol = callManagementProtocol;
            _serviceDiscovery = serviceDiscovery;
        }

        public async Task Run()
        {
            var callManagementTask = _callManagementProtocol.Run();
            var registerTask = Register();

            await TaskCordinator.WaitAll(callManagementTask, registerTask);
        }

        async Task Register()
        {
            while(true)
            {
                var callManagementServerEndpoint = _serviceDiscovery.CallManagementServerEndpoint();
                bool registered = await _callManagementProtocol.RegisterFloorController(
                    _callManagementProtocol.ControlPort, 
                    new IPEndPoint(_serviceDiscovery.GetMyAddress(), 9000), 
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