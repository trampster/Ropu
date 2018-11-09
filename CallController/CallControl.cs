using System.Threading.Tasks;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared;
using System.Net;
using System;

namespace Ropu.CallController
{
    public class CallControl
    {
        readonly LoadBalancerProtocol _loadBalancerProtocol;

        readonly ServiceDiscovery _serviceDiscovery;

        public CallControl(LoadBalancerProtocol loadBalancerProtocol, ServiceDiscovery serviceDiscovery)
        {
            _loadBalancerProtocol = loadBalancerProtocol;
            _serviceDiscovery = serviceDiscovery;
        }

        public async Task Run()
        {
            var loadBalancerTask = _loadBalancerProtocol.Run();
            var registerTask = Register();

            await TaskCordinator.WaitAll(loadBalancerTask, registerTask);
        }

        async Task Register()
        {
            while(true)
            {
                var callManagementServerEndpoint = _serviceDiscovery.CallManagementServerEndpoint();
                bool registered = await _loadBalancerProtocol.SendRegisterCallController(
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