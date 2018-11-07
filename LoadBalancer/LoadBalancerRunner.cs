using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ropu.LoadBalancer.FileServer;
using Ropu.Shared;
using Ropu.Shared.CallManagement;
using Ropu.Shared.ControlProtocol;
using Ropu.Shared.Groups;

namespace Ropu.LoadBalancer
{
    public class LoadBalancerRunner : ICallManagementServerMessageHandler
    {
        readonly CallManagementProtocol _callManagementProtocol;
        readonly ControllerRegistry<RegisteredServingNode> _servingNodes;
        readonly ControllerRegistry<FloorController> _floorControllers;
        readonly IGroupsClient _groupsClient;
        readonly FileManager _fileManager;

        public LoadBalancerRunner(
            CallManagementProtocol callManagementProtocol, 
            IGroupsClient groupsClient, 
            FileManager fileManager)
        {
            _fileManager = fileManager;
            _callManagementProtocol = callManagementProtocol;
            _callManagementProtocol.SetServerMessageHandler(this);
            _groupsClient = groupsClient;

            _servingNodes = new ControllerRegistry<RegisteredServingNode>();

            _floorControllers = new ControllerRegistry<FloorController>();
        }

        public async Task Run()
        {
            var callManagement = _callManagementProtocol.Run();

            await TaskCordinator.WaitAll(callManagement);
        }

        RegisteredServingNode GetServingNode()
        {
            return _servingNodes.GetAvailableController();
        }

        FloorController GetFloorController()
        {
            return _floorControllers.GetAvailableController();
        }

        public void HandleRegisterMediaController(IPAddress fromAddress, ushort requestId, ushort controlPort, IPEndPoint mediaEndpoint)
        {
            Console.WriteLine("Media Controller Registered");
            var endpoint = new IPEndPoint(fromAddress, controlPort);
            _servingNodes.Register(endpoint, controller => controller.Update(mediaEndpoint), () => new RegisteredServingNode(endpoint, mediaEndpoint));
            _callManagementProtocol.SendAck(requestId, endpoint);
        }

        public void HandleRegisterFloorController(IPAddress fromAddress, ushort requestId, ushort controlPort, IPEndPoint floorControlEndpoint)
        {
            Console.WriteLine("Floor Controller Registered");
            var endpoint = new IPEndPoint(fromAddress, controlPort);
            _floorControllers.Register(endpoint, controller => controller.Update(floorControlEndpoint), () => new FloorController(endpoint, floorControlEndpoint));
            _callManagementProtocol.SendAck(requestId, endpoint);
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

            _callManagementProtocol.SendServingNodeResponse(servingNodeEndPoint, endPoint);
        }
    }
}