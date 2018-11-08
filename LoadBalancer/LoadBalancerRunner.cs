using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ropu.LoadBalancer.FileServer;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.ControlProtocol;
using Ropu.Shared.Groups;

namespace Ropu.LoadBalancer
{
    public class LoadBalancerRunner : ILoadBalancerServerMessageHandler
    {
        readonly LoadBalancerProtocol _loadBalancerProtocol;
        readonly ControllerRegistry<RegisteredServingNode> _servingNodes;
        readonly ControllerRegistry<RegisteredCallController> _callControllers;
        readonly IGroupsClient _groupsClient;
        readonly FileManager _fileManager;

        public LoadBalancerRunner(
            LoadBalancerProtocol loadBalancerProtocol, 
            IGroupsClient groupsClient, 
            FileManager fileManager)
        {
            _fileManager = fileManager;
            _loadBalancerProtocol = loadBalancerProtocol;
            _loadBalancerProtocol.SetServerMessageHandler(this);
            _groupsClient = groupsClient;

            _servingNodes = new ControllerRegistry<RegisteredServingNode>();

            _callControllers = new ControllerRegistry<RegisteredCallController>();
        }

        public async Task Run()
        {
            var callManagement = _loadBalancerProtocol.Run();

            await TaskCordinator.WaitAll(callManagement);
        }

        RegisteredServingNode GetServingNode()
        {
            return _servingNodes.GetAvailableController();
        }

        RegisteredCallController GetCallController()
        {
            return _callControllers.GetAvailableController();
        }

        public void HandleRegisterServingNode(IPEndPoint from, ushort requestId, IPEndPoint servingNodeEndpoint)
        {
            Console.WriteLine($"Serving Node Registered at end point {servingNodeEndpoint}");
            _servingNodes.Register(from, controller => controller.Update(servingNodeEndpoint), () => new RegisteredServingNode(from, servingNodeEndpoint));
            _loadBalancerProtocol.SendAck(requestId, from);
        }

        public void HandleRegisterCallController(IPEndPoint from, ushort requestId, IPEndPoint callControlEndpoint)
        {
            Console.WriteLine($"Call Controller Registered at end point {callControlEndpoint}");
            _callControllers.Register(from, controller => controller.Update(callControlEndpoint), () => new RegisteredCallController(from, callControlEndpoint));
            _loadBalancerProtocol.SendAck(requestId, from);
        }

        public void HandleRequestServingNode(ushort requestId, IPEndPoint endPoint)
        {
            var servingNode = _servingNodes.GetAvailableController();
            if(servingNode == null)
            {
                Console.WriteLine("No available serving node"); 
                //TODO: probably should make a packet to indicate this, for now will just let it timeout
                return;
            }
            var servingNodeEndPoint = servingNode.ServingEndPoint;
            Console.WriteLine($"Sending Serving Node Response to {endPoint}"); 

            _loadBalancerProtocol.SendServingNodeResponse(servingNodeEndPoint, endPoint);
        }
    }
}