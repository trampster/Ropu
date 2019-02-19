using System;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;
using Ropu.Shared.LoadBalancing;

namespace Ropu.ServingNode
{
    public class ServingNodeRunner : IMessageHandler, ILoadBalancerClientMessageHandler
    {
        readonly RopuProtocol _ropuProtocol;
        readonly LoadBalancerProtocol _loadBalancerProtocol;
        readonly ServiceDiscovery _serviceDiscovery;
        readonly Registra _registra;
        readonly ServingNodes _servingNodes;
        readonly GroupCallControllerLookup _groupCallControllerLookup;

        uint[] _groupFloorLookup = new uint[ushort.MaxValue];

        public ServingNodeRunner(
            RopuProtocol mediaProtocol, 
            LoadBalancerProtocol loadBalancerProtocol, 
            ServiceDiscovery serviceDiscovery,
            Registra registra,
            ServingNodes servingNodes,
            GroupCallControllerLookup groupCallControllerLookup)
        {
            _ropuProtocol = mediaProtocol;
            _ropuProtocol.SetMessageHandler(this);
            _loadBalancerProtocol = loadBalancerProtocol;
            _serviceDiscovery = serviceDiscovery;
            _registra = registra;
            _servingNodes = servingNodes;
            _groupCallControllerLookup = groupCallControllerLookup;
            loadBalancerProtocol.SetClientMessageHandler(this);
        }

        public async Task Run()
        {
            Task callManagementTask = _loadBalancerProtocol.Run();
            Task mediaTask = _ropuProtocol.Run();
            Task regisrationExpiryTask = _registra.CheckExpiries();

            Task registerTask = Register();


            await TaskCordinator.WaitAll(callManagementTask, mediaTask, registerTask, regisrationExpiryTask);
        }

        public void Registration(uint userId, IPEndPoint endPoint)
        {
            var registration = new Registration(userId, endPoint);
            _registra.Register(registration);

            _ropuProtocol.SendRegisterResponse(registration, endPoint);
        }

        public void HandleCallControllerMessage(ushort groupId, byte[] packetData, int length)
        {
            var endPoint = _groupCallControllerLookup.LookupEndPoint(groupId);
            if(endPoint == null)
            {
                Console.Error.WriteLine($"No Call controller avaialable for group {groupId}");
                return;
            }
            _ropuProtocol.SendPacket(packetData, length, endPoint);
        }

        public void ForwardPacketToClients(ushort groupId, byte[] packetData, int length, IPEndPoint from)
        {
            //forward to clients that have registered with us and belong to that group
            var clientEndPoints = _registra.GetUserEndPoints(groupId);
            if(clientEndPoints == null)
            {
                Console.WriteLine($"No members for group {groupId}");
                return;
            }
            _ropuProtocol.BulkSendAsync(packetData, length, clientEndPoints.GetSpan(), () => clientEndPoints.Release(), from);
        }

        public void ForwardClientMediaPacket(ushort groupId, byte[] packetData, int length, IPEndPoint from)
        {
            //Console.WriteLine("Received media packet");
            //check if it has floor
            uint userId = packetData.AsSpan(5).ParseUint();
            if(_groupFloorLookup[groupId] != userId)
            {
                return;//doesn't have floor
            }

            //forward to all serving nodes
            var servingNodeEndPoints = _servingNodes.EndPoints;
            packetData[0] = (byte)RopuPacketType.MediaPacketGroupCallServingNode;
            _ropuProtocol.BulkSendAsync(packetData, length, servingNodeEndPoints.GetSpan(), () => servingNodeEndPoints.Release());

            //forward to clients
            ForwardPacketToClients(groupId, packetData, length, from);

        }

        async Task Register()
        {
            while(true)
            {
                var callManagementServerEndpoint = _serviceDiscovery.CallManagementServerEndpoint();
                bool registered = await _loadBalancerProtocol.SendRegisterServingNode(
                    new IPEndPoint(_serviceDiscovery.GetMyAddress(), _ropuProtocol.MediaPort), 
                    callManagementServerEndpoint);
                if(registered)
                {
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

        public void HandleGroupCallControllers(ushort requestId, Span<byte> groupCallManagersData)
        {
            for(int index = 0; index < groupCallManagersData.Length; index += 8)
            {
                var groupId = groupCallManagersData.Slice(index).ParseUshort();
                var endPoint = groupCallManagersData.Slice(index+2).ParseIPEndPoint();
                Console.WriteLine($"Added Group Call Controller Group ID: {groupId}, End Point: {endPoint}");
                _groupCallControllerLookup.Add(groupId, endPoint);
            }
            var loadBalancerEndPoint = _serviceDiscovery.CallManagementServerEndpoint();
            _loadBalancerProtocol.SendAck(requestId, loadBalancerEndPoint);
        }

        public void HandleGroupCallControllerRemoved(ushort requestId, ushort groupId)
        {
            Console.WriteLine($"Group Call Controller Removed, Group ID: {groupId}");
            _groupCallControllerLookup.Remove(groupId);
            var loadBalancerEndPoint = _serviceDiscovery.CallManagementServerEndpoint();
            _loadBalancerProtocol.SendAck(requestId, loadBalancerEndPoint);
        }

        public void HandleControllerRegistrationInfo(ushort requestId, byte controllerId, ushort refreshInterval, IPEndPoint endPoint)
        {
        }

        public void Heartbeat(uint userId, IPEndPoint endPoint)
        {
            if(!_registra.UpdateRegistration(userId))
            {
                _ropuProtocol.SendNotRegistered(endPoint);
                return;
            }
            _ropuProtocol.SendHeartbeatResponse(endPoint);
        }

        public void Deregister(uint userId, IPEndPoint endPoint)
        {
            _registra.Deregister(userId);
            _ropuProtocol.SendDeregisterResponse(endPoint);
        }

        public void HandleCallEnded(ushort groupId, byte[] buffer, int length, IPEndPoint endPoint)
        {
            _groupFloorLookup[groupId] = 0;
            ForwardPacketToClients(groupId, buffer, length, endPoint);
        }

        public void ForwardFloorTaken(ushort groupId, byte[] buffer, int length, IPEndPoint endPoint)
        {
            var userId = buffer.AsSpan(3).ParseUint();
            _groupFloorLookup[groupId] = userId;
            ForwardPacketToClients(groupId, buffer, length, endPoint);
        }

        public void ForwardFloorIdle(ushort groupId, byte[] buffer, int length, IPEndPoint endPoint)
        {
            _groupFloorLookup[groupId] = 0;
            ForwardPacketToClients(groupId, buffer, length, endPoint);
        }
    }
}