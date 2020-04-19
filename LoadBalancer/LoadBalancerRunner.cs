using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.Groups;
using Ropu.Shared.Web;
using Ropu.Shared.WebModels;
using System.Threading;

namespace Ropu.LoadBalancer
{
    public class LoadBalancerRunner : ILoadBalancerServerMessageHandler, IGroupCallControllerListener
    {
        readonly LoadBalancerProtocol _loadBalancerProtocol;
        readonly ControllerRegistry<RegisteredServingNode> _servingNodes;
        readonly CallControllerRegistry _callControllers;
        readonly IGroupsClient _groupsClient;
        readonly RopuWebClient _webClient;
        readonly ServicesClient _servicesClient;
        readonly KeysClient _keysClient;
        CommandLineSettings _settings;
        volatile bool _closing = false;

        public LoadBalancerRunner(
            KeysClient keysClient,
            LoadBalancerProtocol loadBalancerProtocol, 
            IGroupsClient groupsClient, 
            RopuWebClient webClient,
            CommandLineSettings settings,
            ServicesClient servicesClient)
        {
            _keysClient = keysClient;
            _settings = settings;
            _loadBalancerProtocol = loadBalancerProtocol;
            _loadBalancerProtocol.SetServerMessageHandler(this);
            _groupsClient = groupsClient;
            _webClient = webClient;
            _servicesClient = servicesClient;

            _servingNodes = new ControllerRegistry<RegisteredServingNode>();

            _callControllers = new CallControllerRegistry(groupsClient);
            _callControllers.SetGroupCallControllerListener(this);
        }

        public async Task Run()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var userId = await _servicesClient.GetUserId(cancellationTokenSource.Token);
            if(userId == null)
            {
                return; // probably cancelled
            }

            await _callControllers.Initialize();

            _loadBalancerProtocol.UserId = userId;

            var keyInfoTask = _keysClient.Run(cancellationTokenSource.Token);
            var callManagement = _loadBalancerProtocol.Run();
            var removeExpired = RemoveExpiredControllers();
            var webdateWeb = UpdateWeb();
            
            var serviceClientTask = _servicesClient.ServiceRegistration(cancellationTokenSource.Token);

            await TaskCordinator.WaitAll(
                keyInfoTask,
                callManagement, 
                removeExpired, 
                webdateWeb,
                serviceClientTask);
        }

        async Task UpdateWeb()
        {
            while(!_closing)
            {  
                var response = await _webClient.Post("api/Services/UpdateLoadBalancer", new LoadBalancerInfo()
                {
                    IPEndPoint = _settings.PublicIPEndpoint
                });

                if(!response.IsSuccessful)
                {
                    Console.Error.WriteLine($"Failed to update the web with the load balancer status {response.StatusCode}");
                }
                await Task.Delay(5000);
            }
        }

        async Task RemoveExpiredControllers()
        {
            while(!_closing)
            {   
                await Task.Delay(5000); //todo: find the time till the next controller expires and wait that long
                _servingNodes.RemoveExpired(removedNode => 
                {
                    var servingNodeEndpoint = removedNode.ServingEndPoint;
                    var existingServingNodeEndPoints = 
                        from node in _servingNodes.GetControllers()
                        select node.ControlEndPoint;
                    var callContollerEndPoints = 
                        from node in _callControllers.Controllers
                        select node.LoadBalancerEndPoint;
                    foreach(var endPoint in existingServingNodeEndPoints.Concat(callContollerEndPoints))
                    {
                        Console.WriteLine($"Sending ServingNodeRemoved to {endPoint}");
                        TaskCordinator.DontWait(() => TaskCordinator.Retry(() => _loadBalancerProtocol.SendServingNodeRemoved(servingNodeEndpoint, endPoint)));
                    } 
                });
                _callControllers.RemoveExpired();
            }
        }

        RegisteredServingNode? GetServingNode()
        {
            return _servingNodes.GetAvailableController();
        }

        public async void HandleRegisterServingNode(IPEndPoint from, ushort requestId, IPEndPoint servingNodeEndpoint)
        {
            Console.WriteLine($"Serving Node Registered at end point {servingNodeEndpoint}");
            bool newNode = _servingNodes.Register(from, controller => controller.Update(servingNodeEndpoint), () => new RegisteredServingNode(from, servingNodeEndpoint));
            _loadBalancerProtocol.SendAck(requestId, from);

            if(!newNode) return;

            await UpdateServingNodes(from, servingNodeEndpoint);
        }

        async Task UpdateServingNodes(IPEndPoint from, IPEndPoint servingNodeEndpoint)
        {
            var existingServingNodeEndPoints = 
                from node in _servingNodes.GetControllers()
                where node.ServingEndPoint != servingNodeEndpoint
                select node.ServingEndPoint;

            //inform that serving node of all existing serving nodes
            await TaskCordinator.Retry(() => _loadBalancerProtocol.SendServingNodes(existingServingNodeEndPoints, from));

            //inform existing serving nodes of the new node
            var existingControlNodeEndPoints = 
                from node in _servingNodes.GetControllers()
                where node.ServingEndPoint != servingNodeEndpoint
                select node.ControlEndPoint;
            var existingCallControllers =
                from controller in _callControllers.Controllers
                select controller.LoadBalancerEndPoint;

            foreach(var endPoint in existingControlNodeEndPoints.Concat(existingCallControllers))
            {
                TaskCordinator.DontWait(() => TaskCordinator.Retry(() => _loadBalancerProtocol.SendServingNodes(new IPEndPoint[]{servingNodeEndpoint}, endPoint)));
            }

            await InformServingNodeOfGroupCallControllers(from);
        }

        async Task InformServingNodeOfGroupCallControllers(IPEndPoint servingNodeEndPoint)
        {
            var managers = _callControllers.GroupCallControllers;
            await TaskCordinator.Retry(() => _loadBalancerProtocol.SendGroupCallControllers(managers, servingNodeEndPoint));
        }

        public async void HandleRegisterCallController(IPEndPoint from, ushort requestId, IPEndPoint callControlEndpoint)
        {
            Console.WriteLine($"Call Controller Registered at end point {callControlEndpoint} from {from}");
            byte? controllerId = _callControllers.Register(from, new RegisteredCallController(from, callControlEndpoint));
            _loadBalancerProtocol.SendAck(requestId, from);

            if(controllerId == null)
            {
                Console.WriteLine("To many call controllers registered");
                //TODO: probably need a response to indicate this
                return;
            }

            await TaskCordinator.Retry(() => _loadBalancerProtocol.SendControllerRegistrationInfo(controllerId.Value, 10, from));

            //inform Call Controller of exiting Serving Nodes
            var existingServingNodeEndPoints = 
                from node in _servingNodes.GetControllers()
                select node.ServingEndPoint;
            await TaskCordinator.Retry(() => _loadBalancerProtocol.SendServingNodes(existingServingNodeEndPoints, from));
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

            _loadBalancerProtocol.SendServingNodeResponse(servingNodeEndPoint, endPoint, requestId);
        }

        public void HandleRefreshCallController(ushort requestId, byte controllerId, IPEndPoint endPoint)
        {
            _callControllers.Refresh(controllerId);
            _loadBalancerProtocol.SendAck(requestId, endPoint);
        }

        public async void GroupsChanged(IEnumerable<GroupCallController> groupCallcontrollers)
        {
            var managers = _callControllers.GroupCallControllers;
            foreach(var servingNode in _servingNodes.GetControllers())
            {
                await TaskCordinator.Retry(() => _loadBalancerProtocol.SendGroupCallControllers(groupCallcontrollers, servingNode.ControlEndPoint));
            }
        }

        public async void GroupCallControllerRemoved(ushort groupId)
        {
            var managers = _callControllers.GroupCallControllers;
            foreach(var servingNode in _servingNodes.GetControllers())
            {
                await TaskCordinator.Retry(() => _loadBalancerProtocol.SendGroupCallControllerRemoved(groupId, servingNode.ControlEndPoint));
            };
        }
    }
}