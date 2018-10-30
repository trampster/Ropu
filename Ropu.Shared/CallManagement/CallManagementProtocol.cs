using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Shared.CallManagement
{
    public class CallManagementProtocol
    {
        readonly Socket _socket;
        SocketAsyncEventArgs _sendArgs;

        readonly ushort _port;
        const int AnyPort = IPEndPoint.MinPort;
        const int MaxUdpSize = 0x10000;
        readonly MemoryPool<byte[]> _sendBufferPool = new MemoryPool<byte[]>(() => new byte[MaxUdpSize]);
        ushort _requestId = 0;


        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);

        ICallManagementServerMessageHandler _serverMessageHandler;
        ICallManagementClientMessageHandler _clientMessageHandler;

        public CallManagementProtocol(ushort port)
        {
            _port = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sendArgs = CreateSocketAsyncEventArgs();
        }

        public ushort ControlPort => _port;

        public void SetServerMessageHandler(ICallManagementServerMessageHandler messageHandler)
        {
            _serverMessageHandler = messageHandler;
        }

        public void SetClientMessageHandler(ICallManagementClientMessageHandler messageHandler)
        {
            _clientMessageHandler = messageHandler;
        }

        public async Task Run()
        {
            var task = new Task(ProcessPackets, TaskCreationOptions.LongRunning);
            task.Start();
            await task;
        }

        void ProcessPackets()
        {
            Console.WriteLine($"Binding call management to port {_port}");
            try
            {
                _socket.Bind(new IPEndPoint(IPAddress.Any, (int)_port));
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

            byte[] _buffer = new byte[MaxUdpSize];
            EndPoint any = Any;

            while(true)
            {
                int ammountRead = _socket.ReceiveFrom(_buffer, ref any);

                var receivedBytes = new Span<byte>(_buffer, 0, ammountRead);
                HandlePacket(receivedBytes, (IPEndPoint)any);
            }
        }

        void HandlePacket(Span<byte> data, IPEndPoint endPoint)
        {
            switch((CallManagementPacketType)data[0])
            {
                case CallManagementPacketType.RegisterMediaController:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort controlPort = data.Slice(3).ParseUshort();
                    var mediaEndpoint = data.Slice(5).ParseIPEndPoint();
                    _serverMessageHandler?.HandleRegisterMediaController(endPoint.Address, requestId, controlPort, mediaEndpoint);
                    break;
                }
                case CallManagementPacketType.RegisterFloorController:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort controlPort = data.Slice(3).ParseUshort();
                    var floorControlEndpoint = data.Slice(5).ParseIPEndPoint();
                    _serverMessageHandler?.HandleRegisterFloorController(endPoint.Address, requestId, controlPort, floorControlEndpoint);
                    break;
                }
                case CallManagementPacketType.StartCall:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort callId = data.Slice(3).ParseUshort();
                    ushort groupId = data.Slice(5).ParseUshort();
                    _clientMessageHandler?.HandleCallStart(requestId, callId, groupId);
                    break;
                }
                case CallManagementPacketType.Ack:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    HandleAck(requestId);
                    break;
                }
                case CallManagementPacketType.RegistrationUpdate:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort groupId = data.Slice(3).ParseUshort();
                    uint userId = data.Slice(5).ParseUint();
                    IPEndPoint regEndPoint = data.Slice(11).ParseIPEndPoint();
                    _clientMessageHandler?.HandleRegistrationUpdate(requestId, groupId, userId, regEndPoint);
                    break;
                }
                case CallManagementPacketType.RegistrationRemoved:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort groupId = data.Slice(3).ParseUshort();
                    uint userId = data.Slice(5).ParseUint();
                    _clientMessageHandler?.HandleRegistrationRemoved(requestId, groupId, userId);
                    break;
                }
                default:
                    throw new NotSupportedException($"PacketType {(CallManagementPacketType)data[0]} was not recognized");
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

        H GetRequestHandler<H>(ushort requestId)
        {
            if(_requests[requestId] == null)
            {
                return default(H);
            }
            return (H)_requests[requestId];
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


        /// <summary>
        /// Index is requestId, value is handler
        /// </summary>
        readonly object[] _requests = new object[ushort.MaxValue];

        async Task<bool> AwaitRequest<H>(
            ushort requestId, H handler, byte[] buffer, IPEndPoint endPoint, 
            ManualResetEvent manualResetEvent, int length)
        {
             _requests[requestId] = handler;

            _socket.SendTo(buffer, 0, length, SocketFlags.None, endPoint);
            bool acknowledged = await AwaitResetEvent(manualResetEvent);

            _requests[requestId] = null;
            return acknowledged;
        }

        public async Task<bool> RegisterMediaController(ushort port, IPEndPoint mediaEndpoint, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)CallManagementPacketType.RegisterMediaController;
            // Request ID (ushort)
            sendBuffer.WriteUshort(requestId, 1);
            // UDP Port (ushort)
            sendBuffer.WriteUshort(port, 3);
            // Media Endpoint
            sendBuffer.WriteEndPoint(mediaEndpoint, 5);

            bool responseReceived = await SendAndWaitForAck(requestId, sendBuffer, 11, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return responseReceived;
        }

        public async Task<bool> RegisterFloorController(ushort port, IPEndPoint floorControlerEndpoint, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)CallManagementPacketType.RegisterFloorController;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            // UDP Port (ushort)
            sendBuffer.WriteUshort(port, 3);
            // Floor Control Endpoint
            sendBuffer.WriteEndPoint(floorControlerEndpoint, 5);

            bool responseReceived = await SendAndWaitForAck(requestId, sendBuffer, 11, targetEndpoint);

            _sendBufferPool.Add(sendBuffer);

            return responseReceived;
        }

        public async Task<bool> StartCall(ushort callId, ushort groupId, IPEndPoint targetEndpoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            ushort requestId = _requestId++;
            // Packet Type 1
            sendBuffer[0] = (byte)CallManagementPacketType.StartCall;
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

        public void SendAck(ushort requestId, IPEndPoint ipEndPoint)
        {
            var sendBuffer = _sendBufferPool.Get();

            sendBuffer[0] = (byte)CallManagementPacketType.Ack;
            // Request ID (uint16)
            sendBuffer.WriteUshort(requestId, 1);
            _socket.SendTo(sendBuffer, 0, 3, SocketFlags.None, ipEndPoint);

            _sendBufferPool.Add(sendBuffer);
        }

        async Task<bool> SendAndWaitForAck(ushort requestId, byte[] buffer, int length, IPEndPoint endpoint)
        {
            var manualResetEvent = new ManualResetEvent(false);

            Action handler = () =>
            {
                manualResetEvent.Set();
            };

            return await AwaitRequest(requestId, handler, buffer, endpoint, manualResetEvent, length);
        }

        async Task<bool> AwaitResetEvent(ManualResetEvent resetEvent)
        {
            return await Task<bool>.Run(() => resetEvent.WaitOne(1000));
        }
    }
}