using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ropu.ControllingFunction.FileServer;
using Ropu.Shared;
using Ropu.Shared.CallManagement;
using Ropu.Shared.ControlProtocol;
using Ropu.Shared.Groups;

namespace Ropu.ControllingFunction
{
    public class ControlFunction : IControlMessageHandler, ICallManagementServerMessageHandler
    {
        readonly ControlProtocol _controlProtocol;
        readonly CallManagementProtocol _callManagementProtocol;
        readonly Registra _registra;
        readonly ControllerRegistry<MediaController> _mediaControllers;
        readonly ControllerRegistry<FloorController> _floorControllers;
        readonly IGroupsClient _groupsClient;
        readonly FileManager _fileManager;
        ushort _callId = 0;

        public ControlFunction(
            ControlProtocol controlProtocol, 
            Registra registra, 
            CallManagementProtocol callManagementProtocol, 
            IGroupsClient groupsClient, 
            FileManager fileManager)
        {
            _fileManager = fileManager;
            _controlProtocol = controlProtocol;
            _controlProtocol.SetMessageHandler(this);
            _callManagementProtocol = callManagementProtocol;
            _callManagementProtocol.SetServerMessageHandler(this);
            _registra = registra;
            _groupsClient = groupsClient;

            _mediaControllers = new ControllerRegistry<MediaController>();

            _floorControllers = new ControllerRegistry<FloorController>();
        }

        public async Task Run()
        {
            var control =  _controlProtocol.Run();
            var callManagement = _callManagementProtocol.Run();

            await TaskCordinator.WaitAll(control, callManagement);
        }

        public void Registration(uint userId, IPEndPoint endPoint)
        {
            var registration = new Registration(userId);
            _registra.Register(registration);
            _controlProtocol.SendRegisterResponse(registration, endPoint);
        }

        public async void StartGroupCall(uint userId, ushort groupId, IPEndPoint endPoint)
        {
            var mediaController = GetMediaController();
            if(mediaController == null)
            {
                _controlProtocol.SendCallStartFailed(CallFailedReason.InsufficientResources, userId, endPoint);
                Console.WriteLine("Can't start call because there is no available MediaController.");
                return;
            }
            var floorController = GetFloorController();
            if(floorController == null)
            {
                _controlProtocol.SendCallStartFailed(CallFailedReason.InsufficientResources, userId, endPoint);
                Console.WriteLine("Can't start call because there is no available FloorController.");
                return;
            }
            var callId = _callId;
            _callId++;
            var mediaTask = SendStartCallWithRetries(callId, groupId, mediaController.ControlEndPoint, 3);
            var floorTask = SendStartCallWithRetries(callId, groupId, floorController.ControlEndPoint, 3);
            //note we start the call without waiting for acks from media and floor controllers
            //this is because we want to minimize latency in call setup. And because we are unlikely to  
            //be losing UDP packets between these backend services.
            SendCallStartedToGroupMembers(userId, callId, groupId, mediaController.MediaEndPoint, floorController.FloorEndPoint);
            if(!await mediaTask || !await floorTask)
            {
                Console.WriteLine("Failed to start call on media controller or floor controller.");
                //should probably end the call...
                return;
            }
        }

        void SendCallStartedToGroupMembers(uint caller, ushort callId, ushort groupId, IPEndPoint mediaController, IPEndPoint floorController)
        {
            //send invite to all group members,
            //we only send one of these, if the miss the CallStarted, then they can request the call 
            //details when the receive floor control or media packets.
            var endPoints = _mediaControllers.GetEndPoints();
            _controlProtocol.SendCallStarted(caller, groupId, callId, mediaController, floorController, endPoints);
        }

        async Task<bool> SendStartCallWithRetries(ushort callId, ushort groupId, IPEndPoint ipEndpoint, int numberOfCalls)
        {
            if(await _callManagementProtocol.StartCall(callId, groupId, ipEndpoint))
            {
                return true;
            }
            numberOfCalls--;
            if(numberOfCalls == 0)
            {
                return false;
            }
            return await SendStartCallWithRetries(callId, groupId, ipEndpoint, numberOfCalls);
        }

        MediaController GetMediaController()
        {
            return _mediaControllers.GetAvailableController();
        }

        FloorController GetFloorController()
        {
            Console.WriteLine("GetFloorController");
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
            _callManagementProtocol.SendServingNodeResponse(servingNodeEndPoint, endPoint);
        }
    }
}