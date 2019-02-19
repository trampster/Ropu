using System.Threading.Tasks;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared;
using System.Net;
using System;

namespace Ropu.CallController
{
    public class CallControl : ILoadBalancerClientMessageHandler, IMessageHandler
    {
        readonly LoadBalancerProtocol _loadBalancerProtocol;
        readonly RopuProtocol _ropuProtocol;
        readonly ServingNodes _servingNodes;

        byte? _controllerId;
        ushort? _refreshInterval;

        readonly ServiceDiscovery _serviceDiscovery;

        readonly GroupCallManager[] _groupCallManagers = new GroupCallManager[ushort.MaxValue];

        public CallControl(
            LoadBalancerProtocol loadBalancerProtocol, 
            ServiceDiscovery serviceDiscovery,
            RopuProtocol ropuProtocol,
            ServingNodes servingNodes)
        {
            _loadBalancerProtocol = loadBalancerProtocol;
            _loadBalancerProtocol.SetClientMessageHandler(this);
            _serviceDiscovery = serviceDiscovery;
            _ropuProtocol = ropuProtocol;
            _ropuProtocol.SetMessageHandler(this);
            _servingNodes = servingNodes;
        }

        public async Task Run()
        {
            var loadBalancerTask = _loadBalancerProtocol.Run();
            var ropuProtocolTask = _ropuProtocol.Run();
            var registerTask = Register();

            await TaskCordinator.WaitAll(loadBalancerTask, registerTask, ropuProtocolTask);
        }

        async Task Register()
        {
            while(true)
            {
                
                var callManagementServerEndpoint = _serviceDiscovery.CallManagementServerEndpoint();
                bool registered = await _loadBalancerProtocol.SendRegisterCallController(
                    new IPEndPoint(_serviceDiscovery.GetMyAddress(), _ropuProtocol.MediaPort), 
                    callManagementServerEndpoint);
                if(registered)
                {
                    Console.WriteLine("Registered");
                    while(true)
                    {
                        await Task.Delay(GetRefreshIntervalMilliseconds());
                        if(_controllerId == null)
                        {
                            //never received a Controller Registration Info Packet
                            Console.WriteLine("Never received a Controller Registration Info Packet");
                            //need to resend our registration
                            break;
                        }
                        if(!await TaskCordinator.Retry(() => _loadBalancerProtocol.SendControllerRefreshCallController(_controllerId.Value, callManagementServerEndpoint)))
                        {
                            registered = false;
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed to register");
                }
            }
        }

        ushort GetRefreshIntervalMilliseconds()
        {
            if(_refreshInterval != null)
            {
                return (ushort)(_refreshInterval.Value * 1000);
            }
            const ushort defaultInterval = 30000;
            return defaultInterval;
        }

        public void HandleCallStart(uint requestId, ushort callId, ushort groupId)
        {
            throw new NotImplementedException();
        }

        public void HandleServingNodes(ushort requestId, Span<byte> nodeEndPointsData)
        {
            _servingNodes.HandleServingNodesPayload(nodeEndPointsData);
            var loadBalancerEndPoint = _serviceDiscovery.CallManagementServerEndpoint();
            _loadBalancerProtocol.SendAck(requestId, loadBalancerEndPoint);
        }

        public void HandleServingNodeRemoved(ushort requestId, IPEndPoint endpoint)
        {
            _servingNodes.RemoveServingNode(endpoint);
            var loadBalancerEndPoint = _serviceDiscovery.CallManagementServerEndpoint();
            _loadBalancerProtocol.SendAck(requestId, loadBalancerEndPoint);
        }

        public void HandleGroupCallControllers(ushort requestId, Span<byte> groupCallControllers)
        {
            throw new NotImplementedException();
        }

        public void HandleGroupCallControllerRemoved(ushort requestId, ushort groupId)
        {
            throw new NotImplementedException();
        }

        public void HandleControllerRegistrationInfo(ushort requestId, byte controllerId, ushort refreshInterval, IPEndPoint endPoint)
        {
            _controllerId = controllerId;
            _refreshInterval = refreshInterval;
            _loadBalancerProtocol.SendAck(requestId, endPoint);
        }

        GroupCallManager GetCallManager(ushort groupId)
        {
            var callManager = _groupCallManagers[groupId];
            if(callManager == null)
            {
                callManager = new GroupCallManager(groupId, _ropuProtocol, _servingNodes);
                _groupCallManagers[groupId] = callManager;
            }
            return callManager;
        }

        public void HandleStartGroupCall(ushort groupId, uint userId)
        {
            var callManager = GetCallManager(groupId);
            callManager.StartCall(userId);
            
        }

        public void HandleFloorReleased(ushort groupId, uint userId)
        {
            var callManager = GetCallManager(groupId);
            callManager.FloorReleased(userId);
        }

        public void HandleFloorRequest(ushort groupId, uint userId)
        {
            var callManager = GetCallManager(groupId);
            callManager.FloorRequest(userId);
        }
    }
}