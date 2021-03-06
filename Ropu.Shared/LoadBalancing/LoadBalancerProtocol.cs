
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ropu.Shared.LoadBalancing
{
    public class LoadBalancerProtocol
    {
        readonly Socket _socket;
        readonly PacketEncryption _packetEncryption;
        readonly KeysClient _keysClient;
        SocketAsyncEventArgs _sendArgs;

        readonly ushort _port;
        const int AnyPort = IPEndPoint.MinPort;
        const int MaxUdpSize = 0x10000;
        readonly MemoryPool<byte[]> _sendBufferPool = new MemoryPool<byte[]>(() => new byte[MaxUdpSize]);
        ushort _requestId = 0;

        readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);

        ILoadBalancerServerMessageHandler? _serverMessageHandler;
        ILoadBalancerClientMessageHandler? _clientMessageHandler;

        public LoadBalancerProtocol(
            IPortFinder portFinder, 
            ushort startingPort, 
            PacketEncryption packetEncryption, 
            KeysClient keysClient)
        {
            _sendArgs = CreateSocketAsyncEventArgs();
            _packetEncryption = packetEncryption;
            _keysClient = keysClient;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _port = (ushort) portFinder.BindToAvailablePort(_socket, IPAddress.Any, startingPort);
            Console.WriteLine($"Load BalancerProtocol bound to port {_port}");
        }

        public ushort ControlPort => _port;

        public void SetServerMessageHandler(ILoadBalancerServerMessageHandler messageHandler)
        {
            _serverMessageHandler = messageHandler;
        }

        public void SetClientMessageHandler(ILoadBalancerClientMessageHandler messageHandler)
        {
            _clientMessageHandler = messageHandler;
        }

        public async Task Run()
        {
            var task = new Task(async () => await ProcessPackets(), TaskCreationOptions.LongRunning);
            task.Start();
            await task;
        }

        async Task ProcessPackets()
        {
            var buffer = new byte[MaxUdpSize];
            var arraySegment = new ArraySegment<byte>(buffer);
            byte[] payload = new byte[MaxUdpSize];
            EndPoint any = Any;

            while(true)
            {
                int read = _socket.ReceiveFrom(buffer, ref any);
                //var result = await _socket.ReceiveFromAsync(arraySegment, SocketFlags.None, any);
                // int read = result.ReceivedBytes;
                int payloadLength = 0;
                try
                {
                    payloadLength = await _packetEncryption.Decrypt(buffer, read, payload);
                }
                catch(Exception exception)
                {
                    Console.Error.WriteLine($"Failed to decrypted packet with exception {exception}");
                    continue;
                }
                
                HandlePacket(payload, payloadLength, (IPEndPoint)any);
            }
        }

        void HandlePacket(byte[] buffer, int ammountRead, IPEndPoint endPoint)
        {
            var data = buffer.AsSpan(0, ammountRead);

            ushort requestId = data.Slice(1).ParseUshort();

            switch((LoadBalancerPacketType)data[0])
            {
                case LoadBalancerPacketType.RegisterServingNode:
                {
                    var mediaEndpoint = data.Slice(3).ParseIPEndPoint();
                    _serverMessageHandler?.HandleRegisterServingNode(endPoint, requestId, mediaEndpoint);
                    break;
                }
                case LoadBalancerPacketType.RegisterCallController:
                {
                    var floorControlEndpoint = data.Slice(3).ParseIPEndPoint();
                    _serverMessageHandler?.HandleRegisterCallController(endPoint, requestId,  floorControlEndpoint);
                    break;
                }
                case LoadBalancerPacketType.ControllerRegistrationInfo:
                {
                    byte controllerId = data[3];
                    ushort refreshInterval = data.Slice(4).ParseUshort();
                    _clientMessageHandler?.HandleControllerRegistrationInfo(requestId, controllerId, refreshInterval, endPoint);
                    break;
                }
                case LoadBalancerPacketType.RefreshCallController:
                {
                    byte controllerId = data[3];
                    _serverMessageHandler?.HandleRefreshCallController(requestId, controllerId, endPoint);
                    break;
                }
                case LoadBalancerPacketType.StartCall:
                {
                    ushort callId = data.Slice(3).ParseUshort();
                    ushort groupId = data.Slice(5).ParseUshort();
                    _clientMessageHandler?.HandleCallStart(requestId, callId, groupId);
                    break;
                }
                case LoadBalancerPacketType.Ack:
                {
                    HandleAck(requestId);
                    break;
                }
                case LoadBalancerPacketType.RequestServingNode:
                {
                    _serverMessageHandler?.HandleRequestServingNode(requestId, endPoint);
                    break;
                }
                case LoadBalancerPacketType.ServingNodeResponse:
                {
                    IPEndPoint servingNodeEndPoint = data.Slice(3).ParseIPEndPoint();
                    GetRequestHandler<Action<IPEndPoint>>(requestId)?.Invoke(servingNodeEndPoint);
                    break;
                }
                case LoadBalancerPacketType.ServingNodes:
                {
                    _clientMessageHandler?.HandleServingNodes(requestId, data.Slice(3));
                    break;
                }
                case LoadBalancerPacketType.ServingNodeRemoved:
                {
                    var servingNodeEndPoint = data.Slice(3).ParseIPEndPoint();
                    _clientMessageHandler?.HandleServingNodeRemoved(requestId, servingNodeEndPoint);
                    break;
                }
                case LoadBalancerPacketType.GroupCallControllers:
                {
                    _clientMessageHandler?.HandleGroupCallControllers(requestId, data.Slice(3));
                    break;
                }
                case LoadBalancerPacketType.GroupCallControllerRemoved:
                {
                    ushort groupId = data.Slice(3).ParseUshort();
                    _clientMessageHandler?.HandleGroupCallControllerRemoved(requestId, groupId);
                    break;    
                }
                default:
                    throw new NotSupportedException($"PacketType {(LoadBalancerPacketType)data[0]} was not recognized");
            }
        }

        void HandleAck(ushort requestId)
        {
            var handler = GetRequestHandler<Action>(requestId);
            if(handler == null)
            {
                return;
            }
            handler();
        }

        H? GetRequestHandler<H>(ushort requestId) where H : class
        {
            var handler = _requests[requestId];
            if(handler == null)
            {
                return default(H);
            }
            return (H)handler;
        }

        SocketAsyncEventArgs CreateSocketAsyncEventArgs()
        {
            var args = new SocketAsyncEventArgs()
            {
                BufferList = new List<ArraySegment<byte>>(),
            };
            args.Completed += (sender, asyncArgs) => SendAsyncCompleted(asyncArgs);
            return args;
        }

        void SendAsyncCompleted(SocketAsyncEventArgs args)
        {
            object token = args.UserToken;
            if(token is Action)
            {
                ((Action)token)();
            }
        }

        void SendAsync(SocketAsyncEventArgs args)
        {
            if(_socket.SendToAsync(args))
            {
                //didn't complete we need to create a new one so we don't interfare
                //TODO: switch to using a pool
                _sendArgs = CreateSocketAsyncEventArgs();
            }
        }

        public uint? UserId
        {
            get;
            set;
        }

        /// <summary>
        /// Index is requestId, value is handler
        /// </summary>
        readonly object?[] _requests = new object[ushort.MaxValue];

        async ValueTask<bool> AwaitRequest<H>(
            ushort requestId, H handler, byte[] buffer, IPEndPoint endPoint, 
            ManualResetEvent manualResetEvent, int length) where H : class
        {
            var userId = UserId;
            var keyInfo = _keysClient.GetMyKeyInfo();
            if(userId == null)
            {
                Console.Error.WriteLine($"Could not send packet because userId is null");
                return false;
            }
            if(keyInfo == null)
            {
                Console.Error.WriteLine($"Could not send packet because keyInfo is null");
                return false;
            }

             _requests[requestId] = handler;

            var packet = _sendBufferPool.Get();
            try
            {
                int packetLength = _packetEncryption.CreateEncryptedPacket(buffer.AsSpan(0, length), packet, false, userId.Value, keyInfo);

                _socket.SendTo(packet, 0, packetLength, SocketFlags.None, endPoint);
                bool acknowledged = await AwaitResetEvent(manualResetEvent);

                _sendBufferPool.Add(packet);

                _requests[requestId] = null;
                return acknowledged;
            }
            catch(Exception exception)
            {
                Console.Error.WriteLine(exception);
                throw exception;
            }


        }

        bool SendToEncrypted(byte[] buffer, int length, IPEndPoint endPoint)
        {
            var userId = UserId;

            var keyInfo = _keysClient.GetMyKeyInfo();

            if(userId == null || keyInfo ==null)
            {
                return false;
            }
            var packet = _sendBufferPool.Get();

            int packetLength = _packetEncryption.CreateEncryptedPacket(buffer.AsSpan(0,  length), packet, false, userId.Value, keyInfo);
            _socket.SendTo(packet, 0, packetLength, SocketFlags.None, endPoint);
            _sendBufferPool.Add(packet);
            return true;
        }

        public async ValueTask<IPEndPoint?> RequestServingNode(IPEndPoint targetEndPoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;

            // Packet Type 6 (byte)
            sendBuffer[0] = (byte)LoadBalancerPacketType.RequestServingNode;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);

            IPEndPoint? servingNodeEndPoint = null;
            var manualResetEvent = new ManualResetEvent(false);

            Action<IPEndPoint> handler = endPoint =>
            {
                servingNodeEndPoint = endPoint;
                manualResetEvent.Set();
            };

            await AwaitRequest(requestId, handler, sendBuffer, targetEndPoint, manualResetEvent, 3);

            return servingNodeEndPoint;
        }

        public void SendServingNodeResponse(IPEndPoint servingNodeEndPoint, IPEndPoint targetEndPoint, ushort requestId)
        {
            var sendBuffer = _sendBufferPool.Get();

            // Packet Type 6 (byte)
            sendBuffer[0] = (byte)LoadBalancerPacketType.ServingNodeResponse;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            // Serving Node Endpoint (6 bytes)
            sendBuffer.WriteEndPoint(servingNodeEndPoint, 3);

            SendToEncrypted(sendBuffer, 9, targetEndPoint);

            _sendBufferPool.Add(sendBuffer);
        }

        public async ValueTask<bool> SendRegisterServingNode(IPEndPoint mediaEndpoint, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)LoadBalancerPacketType.RegisterServingNode;
            // Request ID (ushort)
            sendBuffer.WriteUshort(requestId, 1);
            // Serving Node Endpoint
            sendBuffer.WriteEndPoint(mediaEndpoint, 3);

            bool responseReceived = await SendAndWaitForAck(requestId, sendBuffer, 9, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return responseReceived;
        }

        public async ValueTask<bool> SendRegisterCallController(IPEndPoint callControlerEndpoint, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)LoadBalancerPacketType.RegisterCallController;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            // Call Control Endpoint
            sendBuffer.WriteEndPoint(callControlerEndpoint, 3);

            bool responseReceived = await SendAndWaitForAck(requestId, sendBuffer, 9, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return responseReceived;
        }

        public async ValueTask<bool> SendServingNodes(IEnumerable<IPEndPoint> endPoints, IPEndPoint targetEndpoint)
        {
             var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)LoadBalancerPacketType.ServingNodes;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            // Serving Node EndPoint(s)
            int index = 3;
            foreach(var endPoint in endPoints)
            {
                sendBuffer.WriteEndPoint(endPoint, index);
                index += 6;
            }

            bool responseReceived = await SendAndWaitForAck(requestId, sendBuffer, index, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return responseReceived;
        }

        public async ValueTask<bool> SendServingNodeRemoved(IPEndPoint servinNodeEndPoint, IPEndPoint targetEndpoint)
        {
             var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)LoadBalancerPacketType.ServingNodeRemoved;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            // Serving Node EndPoint (6 bytes)
            sendBuffer.WriteEndPoint(servinNodeEndPoint, 3);

            bool responseReceived = await SendAndWaitForAck(requestId, sendBuffer, 9, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return responseReceived;
        }

        public async ValueTask<bool> SendGroupCallControllers(IEnumerable<GroupCallController> callManagers, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)LoadBalancerPacketType.GroupCallControllers;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            // Group Call Manager(s)
            int index = 3;
            foreach(var callManager in callManagers)
            {
                sendBuffer.WriteUshort(callManager.GroupId, index);
                index += 2;
                sendBuffer.WriteEndPoint(callManager.EndPoint, index);
                index += 6;
            }

            bool responseReceived = await SendAndWaitForAck(requestId, sendBuffer, index, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return responseReceived;
        }

        public async ValueTask<bool> SendGroupCallControllerRemoved(ushort groupId, IPEndPoint targetEndpoint)
        {
             var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)LoadBalancerPacketType.GroupCallControllerRemoved;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            // Group ID (2 bytes)
            sendBuffer.WriteUshort(groupId, 3);

            bool responseReceived = await SendAndWaitForAck(requestId, sendBuffer, 5, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return responseReceived;
        }

        public async ValueTask<bool> StartCall(ushort callId, ushort groupId, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)LoadBalancerPacketType.StartCall;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            // Call ID (uint16)
            sendBuffer.WriteUshort(callId, 3);
            // Group ID (uint16)
            sendBuffer.WriteUshort(groupId, 5);

            bool repsonseReceived = await SendAndWaitForAck(requestId, sendBuffer, 7, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return repsonseReceived;
        }

        public async ValueTask<bool> SendControllerRegistrationInfo(byte controllerId, ushort refreshInterval, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type
            sendBuffer[0] = (byte)LoadBalancerPacketType.ControllerRegistrationInfo;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            //Controller ID (byte) - to be included in the Refresh Controller packet
            sendBuffer[3] = controllerId;
            // Refresh Interval (ushort) - in seconds
            sendBuffer.WriteUshort(refreshInterval, 4);

            bool repsonseReceived = await SendAndWaitForAck(requestId, sendBuffer, 6, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return repsonseReceived;
        }

        public async ValueTask<bool> SendControllerRefreshCallController (byte controllerId, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type
            sendBuffer[0] = (byte)LoadBalancerPacketType.RefreshCallController;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            //Controller ID (byte) 
            sendBuffer[3] = controllerId;

            bool repsonseReceived = await SendAndWaitForAck(requestId, sendBuffer, 4, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return repsonseReceived;
        }

        public void SendAck(ushort requestId, IPEndPoint ipEndPoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            sendBuffer[0] = (byte)LoadBalancerPacketType.Ack;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);

            SendToEncrypted(sendBuffer, 3, ipEndPoint);

            _sendBufferPool.Add(sendBuffer);
        }

        async ValueTask<bool> SendAndWaitForAck(ushort requestId, byte[] buffer, int length, IPEndPoint endpoint)
        {
            var manualResetEvent = new ManualResetEvent(false);

            Action handler = () =>
            {
                manualResetEvent.Set();
            };

            return await AwaitRequest(requestId, handler, buffer, endpoint, manualResetEvent, length);
        }

        async ValueTask<bool> AwaitResetEvent(ManualResetEvent resetEvent)
        {
            return await Task<bool>.Run(() => resetEvent.WaitOne(1000));
        }
    }
}