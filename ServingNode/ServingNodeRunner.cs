using System;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;

namespace Ropu.ServingNode
{
    public class ServingNodeRunner : IMessageHandler, ILoadBalancerClientMessageHandler
    {
        readonly MediaProtocol _mediaProtocol;
        readonly LoadBalancerProtocol _loadBalancerProtocol;
        readonly ServiceDiscovery _serviceDiscovery;
        readonly Registra _registra;
        readonly ServingNodes _servingNodes;

        public ServingNodeRunner(
            MediaProtocol mediaProtocol, 
            LoadBalancerProtocol loadBalancerProtocol, 
            ServiceDiscovery serviceDiscovery,
            Registra registra,
            ServingNodes servingNodes)
        {
            _mediaProtocol = mediaProtocol;
            _mediaProtocol.SetMessageHandler(this);
            _loadBalancerProtocol = loadBalancerProtocol;
            _serviceDiscovery = serviceDiscovery;
            _registra = registra;
            _servingNodes = servingNodes;
            loadBalancerProtocol.SetClientMessageHandler(this);
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

        public void HandleServingNodes(ushort requestId, Span<byte> nodeEndPointsData)
        {
            _servingNodes.HandleServingNodesPayload(nodeEndPointsData);
            var loadBalancerEndPoint = _serviceDiscovery.CallManagementServerEndpoint();
            _loadBalancerProtocol.SendAck(requestId, loadBalancerEndPoint);
        }

        public void HandleServingNodeRemoved(ushort requestId, IPEndPoint endpoint)
        {
            Console.WriteLine($"Serving Node Removed {endpoint}");
            _servingNodes.RemoveServingNode(endpoint);
            var loadBalancerEndPoint = _serviceDiscovery.CallManagementServerEndpoint();
            _loadBalancerProtocol.SendAck(requestId, loadBalancerEndPoint);
        }

        public void HandleCallStart(uint requestId, ushort callId, ushort groupId)
        {
            throw new NotImplementedException();
        }

        public void HandleGroupCallManagers(ushort requestId, Span<byte> groupCallManagers)
        {
            throw new NotImplementedException();
        }

        public void HandleGroupCallManagerRemoved(ushort requestId, ushort groupId)
        {
            throw new NotImplementedException();
        }
    }
}