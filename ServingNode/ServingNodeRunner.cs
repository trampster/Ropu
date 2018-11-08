using System;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;

namespace Ropu.ServingNode
{
    public class ServingNodeRunner : IMessageHandler
    {
        readonly MediaProtocol _mediaProtocol;
        readonly LoadBalancerProtocol _loadBalancerProtocol;
        readonly ServiceDiscovery _serviceDiscovery;
        readonly Registra _registra;

        public ServingNodeRunner(
            MediaProtocol mediaProtocol, 
            LoadBalancerProtocol loadBalancerProtocol, 
            ServiceDiscovery serviceDiscovery,
            Registra registra)
        {
            _mediaProtocol = mediaProtocol;
            _mediaProtocol.SetMessageHandler(this);
            _loadBalancerProtocol = loadBalancerProtocol;
            _serviceDiscovery = serviceDiscovery;
            _registra = registra;
        }

        public async Task Run()
        {
            Task callManagementTask = _loadBalancerProtocol.Run();
            Task mediaTask = _mediaProtocol.Run();

            Task registerTask = Register();


            await TaskCordinator.WaitAll(callManagementTask, mediaTask, registerTask);
        }

        public void Registration(uint userId, IPEndPoint endPoint)
        {
            var registration = new Registration(userId, endPoint);
            _registra.Register(registration);
            _mediaProtocol.SendRegisterResponse(registration, endPoint);
        }

        public void StartGroupCall(uint userId, ushort groupId, IPEndPoint endPoint)
        {

        }

        async Task Register()
        {
            while(true)
            {
                var callManagementServerEndpoint = _serviceDiscovery.CallManagementServerEndpoint();
                bool registered = await _loadBalancerProtocol.SendRegisterServingNode(
                    new IPEndPoint(_serviceDiscovery.GetMyAddress(), _mediaProtocol.MediaPort), 
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