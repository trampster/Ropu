using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared.CallManagement;
using Ropu.Shared.ControlProtocol;
using Ropu.Shared.Groups;

namespace Ropu.ControllingFunction
{
    public class Controller : IControlMessageHandler, ICallManagementServerMessageHandler
    {
        readonly ControlProtocol _controlProtocol;

        readonly CallManagementProtocol _callManagementProtocol;
        readonly Registra _registra;
        readonly List<MediaController> _mediaControllers;
        readonly List<FloorController> _floorControllers;
        readonly IGroupsClient _groupsClient;
        ushort _callId = 0;

        public Controller(ControlProtocol controlProtocol, Registra registra, CallManagementProtocol callManagementProtocol, IGroupsClient groupsClient)
        {
            _controlProtocol = controlProtocol;
            _controlProtocol.SetMessageHandler(this);
            _callManagementProtocol = callManagementProtocol;
            _callManagementProtocol.SetServerMessageHandler(this);
            _registra = registra;
            _groupsClient = groupsClient;

            _mediaControllers = new List<MediaController>();
            _floorControllers = new List<FloorController>();
        }

        public void Run()
        {
            _controlProtocol.ProcessPackets();
        }

        public void Registration(uint userId, ushort rtpPort, ushort floorControlPort, IPEndPoint controlEndpoint)
        {
            var registration = new Registration(userId, rtpPort, floorControlPort, controlEndpoint);
            _registra.Register(registration);
            _controlProtocol.SendRegisterResponse(registration);
        }

        public async void StartGroupCall(uint userId, ushort groupId)
        {
            var mediaController = GetMediaController();
            if(mediaController == null)
            {
                _controlProtocol.SendCallStartFailed(CallFailedReason.InsufficientResources, _registra.GetEndPoint(userId));
                Console.WriteLine("Can't start call because there is no available MediaController.");
                return;
            }
            var floorController = GetFloorController();
            if(floorController == null)
            {
                _controlProtocol.SendCallStartFailed(CallFailedReason.InsufficientResources, _registra.GetEndPoint(userId));
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
            var endPoints = _groupsClient.GetGroupMemberEndpoints(groupId);
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
            return _mediaControllers.FirstOrDefault(); //TODO: choose one that isn't being used
        }

        FloorController GetFloorController()
        {
            return _floorControllers.FirstOrDefault(); //TODO: choose one that isn't being used
        }

        public void HandleRegisterMediaController(IPAddress fromAddress, uint requestId, ushort controlPort, IPEndPoint mediaEndpoint)
        {
            var endpoint = new IPEndPoint(fromAddress, controlPort);
            _mediaControllers.Add(new MediaController(endpoint, mediaEndpoint));
            _callManagementProtocol.SendAck(requestId, endpoint);
        }

        public void HandleRegisterFloorController(IPAddress fromAddress, uint requestId, ushort controlPort, IPEndPoint floorControlEndpoint)
        {
            var endpoint = new IPEndPoint(fromAddress, controlPort);
            _floorControllers.Add(new FloorController(endpoint, floorControlEndpoint));
            _callManagementProtocol.SendAck(requestId, endpoint);
        }
    }
}