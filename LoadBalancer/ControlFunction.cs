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
    public class ControlFunction : ICallManagementServerMessageHandler
    {
        readonly CallManagementProtocol _callManagementProtocol;
        readonly ControllerRegistry<MediaController> _mediaControllers;
        readonly ControllerRegistry<FloorController> _floorControllers;
        readonly IGroupsClient _groupsClient;
        readonly FileManager _fileManager;
        ushort _callId = 0;

        public ControlFunction(
            CallManagementProtocol callManagementProtocol, 
            IGroupsClient groupsClient, 
            FileManager fileManager)
        {
            _fileManager = fileManager;
            _callManagementProtocol = callManagementProtocol;
            _callManagementProtocol.SetServerMessageHandler(this);
            _groupsClient = groupsClient;

            _mediaControllers = new ControllerRegistry<MediaController>();

            _floorControllers = new ControllerRegistry<FloorController>();
        }

        public async Task Run()
        {
            var callManagement = _callManagementProtocol.Run();

            await TaskCordinator.WaitAll(callManagement);
        }

        MediaController GetMediaController()
        {
            return _mediaControllers.GetAvailableController();
        }

        FloorController GetFloorController()
        {
            return _floorControllers.GetAvailableController();
        }

        public void HandleRegisterMediaController(IPAddress fromAddress, ushort requestId, ushort controlPort, IPEndPoint mediaEndpoint)
        {
            Console.WriteLine("Media Controller Registered");
            var endpoint = new IPEndPoint(fromAddress, controlPort);
            _mediaControllers.Register(endpoint, controller => controller.Update(mediaEndpoint), () => new MediaController(endpoint, mediaEndpoint));
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
            var servingNode = _mediaControllers.GetAvailableController();
            if(servingNode == null)
            {
                Console.WriteLine("No available serving node"); 
                //TODO: probably should make a packet to indicate this, for now will just let it timeout
                return;
            }
            var servingNodeEndPoint = servingNode.MediaEndPoint;
            Console.WriteLine($"Sending Serving Node Response to {endPoint}"); 

            _callManagementProtocol.SendServingNodeResponse(servingNodeEndPoint, endPoint);
        }
    }
}