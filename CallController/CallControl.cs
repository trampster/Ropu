using System.Threading.Tasks;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared;
using System.Net;
using System;

namespace Ropu.CallController
{
    public class CallControl : ILoadBalancerClientMessageHandler
    {
        readonly LoadBalancerProtocol _loadBalancerProtocol;
        readonly RopuProtocol _ropuProtocol;
        byte? _controllerId;
        ushort? _refreshInterval;

        readonly ServiceDiscovery _serviceDiscovery;

        public CallControl(
            LoadBalancerProtocol loadBalancerProtocol, 
            ServiceDiscovery serviceDiscovery,
            RopuProtocol ropuProtocol)
        {
            _loadBalancerProtocol = loadBalancerProtocol;
            _loadBalancerProtocol.SetClientMessageHandler(this);
            _serviceDiscovery = serviceDiscovery;
            _ropuProtocol = ropuProtocol;
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
                        Console.WriteLine("Refreshing registration");
                        await TaskCordinator.Retry(() => _loadBalancerProtocol.SendControllerRefreshCallController(_controllerId.Value, callManagementServerEndpoint));
                        continue;
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
            if(_refreshInterval == null)
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
            throw new NotImplementedException();
        }

        public void HandleServingNodeRemoved(ushort requestId, IPEndPoint endpoint)
        {
            throw new NotImplementedException();
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
    }
}