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
        readonly ushort _port;
        const int AnyPort = IPEndPoint.MinPort;
        const int MaxUdpSize = 0x10000;
        readonly byte[] _sendBuffer = new byte[MaxUdpSize];
        ushort _requestId = 0;


        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);

        ICallManagementServerMessageHandler _serverMessageHandler;
        ICallManagementClientMessageHandler _clientMessageHandler;

        public CallManagementProtocol(ushort port)
        {
            _port = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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
                case CallManagementPacketType.GetGroupsFileRequest:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    _serverMessageHandler?.HandleGetGroupsFileRequest(endPoint, requestId);
                    break;
                }
                case CallManagementPacketType.GetGroupFileRequest:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort groupId = data.Slice(3).ParseUshort();
                    _serverMessageHandler?.HandleGetGroupFileRequest(endPoint, requestId, groupId);
                    break;
                }
                case CallManagementPacketType.FileManifestResponse:
                {
                    Console.WriteLine("Received File Manifest Response");
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort numberOfParts = data.Slice(3).ParseUshort();
                    ushort fileId = data.Slice(5).ParseUshort();
                    var handler = GetRequestHandler<Action<ushort, ushort>>(requestId);
                    if(handler != null)
                    {
                        Console.WriteLine("Calling handler");
                        handler(numberOfParts, fileId);
                    }
                    break;
                }
                case CallManagementPacketType.FilePartRequest:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort fileId = data.Slice(3).ParseUshort();
                    ushort partNumber = data.Slice(5).ParseUshort();
                    Console.WriteLine("Receiving file part request");
                    _serverMessageHandler?.HandleFilePartRequest(endPoint, requestId, fileId, partNumber);
                    break;
                }
                case CallManagementPacketType.FilePartResponse:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    var payload = data.Slice(3);
                    Console.WriteLine("Receiving file part response");
                    var handler = GetRequestHandler<ReadOnlySpanAction<byte, FilePartFailureReason>>(requestId);
                    handler(payload, FilePartFailureReason.Success);
                    _clientMessageHandler?.HandleFilePartResponse(requestId, payload);
                    break;
                }
                case CallManagementPacketType.FilePartUnrecognized:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    FilePartFailureReason reason = (FilePartFailureReason)data[3];
                    _clientMessageHandler?.HandleFilePartUnrecognized(requestId, reason);
                    break;
                }
                case CallManagementPacketType.CompleteFileTransfer:
                {
                    ushort requestId = data.Slice(1).ParseUshort();
                    ushort fileId = data.Slice(3).ParseUshort();
                    _serverMessageHandler?.HandleCompleteFileTransfer(endPoint, requestId, fileId);
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

        public void SendFilePartUnrecognized(ushort requestId, FilePartFailureReason reason, IPEndPoint ipEndPoint)
        {
            // Packet Type 
            _sendBuffer[0] = (byte)CallManagementPacketType.FilePartUnrecognized;
            // Request ID (ushort)
            _sendBuffer.WriteUshort(requestId, 1);
            // Reason Port (byte)
            _sendBuffer[3] = (byte)reason;

            _socket.SendTo(_sendBuffer, 0, 4, SocketFlags.None, ipEndPoint);
        }

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs()
        {
            BufferList = new List<ArraySegment<byte>>()
        };

        void SendAsync(SocketAsyncEventArgs args)
        {
            if(_socket.SendToAsync(args))
            {
                Console.WriteLine($"Didn't complete");

                //didn't complete we need to create a new one so we don't interfare
                var newSendArgs = new SocketAsyncEventArgs();
                newSendArgs.BufferList = new List<ArraySegment<byte>>();
                _sendArgs = newSendArgs;
            }
            Console.WriteLine($"Bytes Transfered {args.BytesTransferred}");
            Console.WriteLine($"SocketError {args.SocketError}");
        }

        public void SendFilePartResponse(ushort requestId, ArraySegment<byte> payload, IPEndPoint ipEndPoint)
        {
            // Packet Type 
            _sendBuffer[0] = (byte)CallManagementPacketType.FilePartResponse;
            // Request ID (uint16)
            _sendBuffer.WriteUshort(requestId, 1);

            var headerSegment = new ArraySegment<byte>(_sendBuffer, 0, 3);
            _sendArgs.RemoteEndPoint = ipEndPoint;
            var bufferList = _sendArgs.BufferList;
            bufferList.Clear();

            bufferList.Add(headerSegment);
            bufferList.Add(payload);
            _sendArgs.BufferList = bufferList;
            Console.WriteLine($"SendFilePartResponse: SendAsync to {_sendArgs.RemoteEndPoint}");
            SendAsync(_sendArgs);
        }


        /// <summary>
        /// Index is requestId, value is handler
        /// </summary>
        readonly object[] _requests = new object[ushort.MaxValue];

        public async Task<bool> SendGetGroupsFileRequest(IPEndPoint targetEndpoint, Action<ushort,ushort> handler)
        {
            ushort requestId = _requestId++;
            // Packet Type 5 (byte)
            _sendBuffer[0] = (byte)CallManagementPacketType.GetGroupsFileRequest;
            // Request ID (uint16)
            _sendBuffer.WriteUshort(requestId, 1);

            var manualResetEvent = new ManualResetEvent(false); //TODO: get from pool

            Action<ushort,ushort> handler1 = (numberOfParts, fileId) =>
            {
                handler(numberOfParts, fileId);
                manualResetEvent.Set();
            };

            return await AwaitRequest(requestId, handler1, _sendBuffer, targetEndpoint, manualResetEvent, 3);
        }

        public async Task<bool> SendGetFilePartRequest(ushort fileId, ushort partNumber, ReadOnlySpanAction<byte, FilePartFailureReason> handler, IPEndPoint targetEndpoint)
        {
            ushort requestId = _requestId++;
            // Packet Type 7 (byte)
            _sendBuffer[0] = (byte)CallManagementPacketType.FilePartRequest;
            // Request ID (uint16)
            _sendBuffer.WriteUshort(requestId, 1);
            // File ID (uint16)
            _sendBuffer.WriteUshort(fileId, 3);
            // Part Number (uint16)
            _sendBuffer.WriteUshort(partNumber, 5);

            var manualResetEvent = new ManualResetEvent(false); //TODO: get from pool

            ReadOnlySpanAction<byte, FilePartFailureReason> handler1 = (packet, failureReason) =>
            {
                handler(packet, failureReason);
                manualResetEvent.Set();
            };

            return await AwaitRequest(requestId, handler1, _sendBuffer, targetEndpoint, manualResetEvent, 7);
        }

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
            ushort requestId = _requestId++;
            // Packet Type 1
            _sendBuffer[0] = (byte)CallManagementPacketType.RegisterMediaController;
            // Request ID (ushort)
            _sendBuffer.WriteUshort(requestId, 1);
            // UDP Port (ushort)
            _sendBuffer.WriteUshort(port, 3);
            // Media Endpoint
            _sendBuffer.WriteEndPoint(mediaEndpoint, 5);

            return await SendAndWaitForAck(requestId, _sendBuffer, 11, targetEndpoint);
        }

        public async Task<bool> RegisterFloorController(ushort port, IPEndPoint floorControlerEndpoint, IPEndPoint targetEndpoint)
        {
            ushort requestId = _requestId++;
            // Packet Type 1
            _sendBuffer[0] = (byte)CallManagementPacketType.RegisterFloorController;
            // Request ID (uint16)
            _sendBuffer.WriteUshort(requestId, 1);
            // UDP Port (ushort)
            _sendBuffer.WriteUshort(port, 3);
            // Floor Control Endpoint
            _sendBuffer.WriteEndPoint(floorControlerEndpoint, 5);

            return await SendAndWaitForAck(requestId, _sendBuffer, 11, targetEndpoint);
        }

        public async Task<bool> StartCall(ushort callId, ushort groupId, IPEndPoint targetEndpoint)
        {
            ushort requestId = _requestId++;
            // Packet Type 1
            _sendBuffer[0] = (byte)CallManagementPacketType.StartCall;
            // Request ID (uint16)
            _sendBuffer.WriteUshort(requestId, 1);
            // Call ID (uint16)
            _sendBuffer.WriteUshort(callId, 3);
            // Group ID (uint16)
            _sendBuffer.WriteUshort(groupId, 5);

            return await SendAndWaitForAck(requestId, _sendBuffer, 7, targetEndpoint);
        }

        public void SendAck(ushort requestId, IPEndPoint ipEndPoint)
        {
            _sendBuffer[0] = (byte)CallManagementPacketType.Ack;
            // Request ID (uint16)
            _sendBuffer.WriteUshort(requestId, 1);
            _socket.SendTo(_sendBuffer, 0, 3, SocketFlags.None, ipEndPoint);
        }

        public void SendFileManifestResponse(ushort requestId, ushort numberOfParts, ushort fileId, IPEndPoint ipEndPoint)
        {
            _sendBuffer[0] = (byte)CallManagementPacketType.FileManifestResponse;
            // Request ID (uint16)
            _sendBuffer.WriteUshort(requestId, 1);
            // Number of Parts (uint16)
            _sendBuffer.WriteUshort(numberOfParts, 3);
            // File ID (uint16)
            _sendBuffer.WriteUshort(fileId, 5);
            _socket.SendTo(_sendBuffer, 0, 7, SocketFlags.None, ipEndPoint);
        }

        async Task<bool> SendAndWaitForAck(ushort requestId, byte[] buffers, int length, IPEndPoint endpoint)
        {
            var manualResetEvent = new ManualResetEvent(false);

            Action handler = () =>
            {
                manualResetEvent.Set();
            };

            return await AwaitRequest(requestId, handler, _sendBuffer, endpoint, manualResetEvent, length);
        }

        async Task<bool> AwaitResetEvent(ManualResetEvent resetEvent)
        {
            return await Task<bool>.Run(() => resetEvent.WaitOne(1000));
        }
    }
}