using System;
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
        uint _requestId = 0;


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
                    uint requestId = data.Slice(1).ParseUint();
                    ushort controlPort = data.Slice(5).ParseUshort();
                    var mediaEndpoint = data.Slice(7).ParseIPEndPoint();
                    _serverMessageHandler?.HandleRegisterMediaController(endPoint.Address, requestId, controlPort, mediaEndpoint);
                    break;
                }
                case CallManagementPacketType.RegisterFloorController:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort controlPort = data.Slice(5).ParseUshort();
                    var floorControlEndpoint = data.Slice(7).ParseIPEndPoint();
                    _serverMessageHandler?.HandleRegisterFloorController(endPoint.Address, requestId, controlPort, floorControlEndpoint);
                    break;
                }
                case CallManagementPacketType.StartCall:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort callId = data.Slice(5).ParseUshort();
                    ushort groupId = data.Slice(7).ParseUshort();
                    _clientMessageHandler?.HandleCallStart(requestId, callId, groupId);
                    break;
                }
                case CallManagementPacketType.Ack:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    HandleAck(requestId);
                    break;
                }
                case CallManagementPacketType.GetGroupsFileRequest:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    _serverMessageHandler?.HandleGetGroupsFileRequest(endPoint, requestId);
                    break;
                }
                case CallManagementPacketType.GroupGroupFileRequest:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort groupId = data.Slice(5).ParseUshort();
                    _serverMessageHandler?.HandleGetGroupFileRequest(endPoint, requestId, groupId);
                    break;
                }
                case CallManagementPacketType.FileManifestResponse:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort numberOfParts = data.Slice(5).ParseUshort();
                    ushort fileId = data.Slice(7).ParseUshort();
                    _clientMessageHandler?.HandleFileManifestResponse(requestId, numberOfParts, fileId);
                    break;
                }
                case CallManagementPacketType.FilePartRequest:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort fileId = data.Slice(5).ParseUshort();
                    ushort partNumber = data.Slice(7).ParseUshort();
                    _serverMessageHandler?.HandleFilePartRequest(endPoint, requestId, fileId, partNumber);
                    break;
                }
                case CallManagementPacketType.FilePartResponse:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    var payload = data.Slice(5);
                    _clientMessageHandler?.HandleFilePartResponse(requestId, payload);
                    break;
                }
                case CallManagementPacketType.FilePartUnrecognized:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    FilePartFailureReason reason = (FilePartFailureReason)data[5];
                    _clientMessageHandler?.HandleFilePartUnrecognized(requestId, reason);
                    break;
                }
                case CallManagementPacketType.CompleteFileTransfer:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort fileId = data.Slice(5).ParseUshort();
                    _serverMessageHandler?.HandleCompleteFileTransfer(endPoint, requestId, fileId);
                    break;
                }
                case CallManagementPacketType.RegistrationUpdate:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort groupId = data.Slice(5).ParseUshort();
                    uint userId = data.Slice(7).ParseUint();
                    IPEndPoint regEndPoint = data.Slice(11).ParseIPEndPoint();
                    _clientMessageHandler?.HandleRegistrationUpdate(requestId, groupId, userId, regEndPoint);
                    break;
                }
                case CallManagementPacketType.RegistrationRemoved:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort groupId = data.Slice(5).ParseUshort();
                    uint userId = data.Slice(7).ParseUint();
                    _clientMessageHandler?.HandleRegistrationRemoved(requestId, groupId, userId);
                    break;
                }
                default:
                    throw new NotSupportedException($"PacketType {(CallManagementPacketType)data[0]} was not recognized");
            }
        }

        void HandleAck(uint requestId)
        {
            if(_waitingRequests.TryGetValue(requestId, out ManualResetEvent resetEvent))
            {
                resetEvent.Set();
            }
        }

        public void SendFilePartUnrecognized(uint requestId, FilePartFailureReason reason, IPEndPoint ipEndPoint)
        {
            // Packet Type 
            _sendBuffer[0] = (byte)CallManagementPacketType.FilePartUnrecognized;
            // Request ID (uint32)
            _sendBuffer.WriteUint(requestId, 1);
            // Reason Port (byte)
            _sendBuffer[5] = (byte)reason;

            _socket.SendTo(_sendBuffer, 0, 6, SocketFlags.None, ipEndPoint);
        }

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();

        void SendAsync(SocketAsyncEventArgs args)
        {
            if(_socket.SendAsync(_sendArgs))
            {
                //didn't complete we need to create a new one so we don't interfare
                var newSendArgs = new SocketAsyncEventArgs();
                newSendArgs.BufferList = new List<ArraySegment<byte>>();
                _sendArgs = newSendArgs;
            }
        }

        public void SendFilePartResponse(uint requestId, ArraySegment<byte> payload, IPEndPoint ipEndPoint)
        {
            // Packet Type 
            _sendBuffer[0] = (byte)CallManagementPacketType.FilePartUnrecognized;
            // Request ID (uint32)
            _sendBuffer.WriteUint(requestId, 1);

            var headerSegment = new ArraySegment<byte>(_sendBuffer, 0, 5);

            _sendArgs.RemoteEndPoint = ipEndPoint;
            var bufferList = _sendArgs.BufferList;
            bufferList.Clear();
            bufferList.Add(headerSegment);
            bufferList.Add(payload);

            SendAsync(_sendArgs);
        }

        public async Task<bool> RegisterMediaController(ushort port, IPEndPoint mediaEndpoint, IPEndPoint targetEndpoint)
        {
            uint requestId = _requestId++;
            // Packet Type 1
            _sendBuffer[0] = (byte)CallManagementPacketType.RegisterMediaController;
            // Request ID (uint32)
            _sendBuffer.WriteUint(requestId, 1);
            // UDP Port (ushort)
            _sendBuffer.WriteUshort(port, 5);
            // Media Endpoint
            _sendBuffer.WriteEndPoint(mediaEndpoint, 7);

            return await Send(requestId, _sendBuffer, 13, targetEndpoint);
        }

        public async Task<bool> RegisterFloorController(ushort port, IPEndPoint floorControlerEndpoint, IPEndPoint targetEndpoint)
        {
            uint requestId = _requestId++;
            // Packet Type 1
            _sendBuffer[0] = (byte)CallManagementPacketType.RegisterFloorController;
            // Request ID (uint32)
            _sendBuffer.WriteUint(requestId, 1);
            // UDP Port (ushort)
            _sendBuffer.WriteUshort(port, 5);
            // Floor Control Endpoint
            _sendBuffer.WriteEndPoint(floorControlerEndpoint, 7);

            return await Send(requestId, _sendBuffer, 13, targetEndpoint);
        }

        public async Task<bool> StartCall(ushort callId, ushort groupId, IPEndPoint targetEndpoint)
        {
            uint requestId = _requestId++;
            // Packet Type 1
            _sendBuffer[0] = (byte)CallManagementPacketType.StartCall;
            // Request ID (uint32)
            _sendBuffer.WriteUint(requestId, 1);
            // Call ID (uint16)
            _sendBuffer.WriteUshort(callId, 5);
            // Group ID (uint16)
            _sendBuffer.WriteUshort(groupId, 7);

            return await Send(requestId, _sendBuffer, 9, targetEndpoint);
        }


        public void SendAck(uint requestId, IPEndPoint ipEndPoint)
        {
            _sendBuffer[0] = (byte)CallManagementPacketType.Ack;
            // Request ID (uint32)
            _sendBuffer.WriteUint(requestId, 1);
            _socket.SendTo(_sendBuffer, 0, 5, SocketFlags.None, ipEndPoint);
        }

        public void SendFileManifestResponse(uint requestId, ushort numberOfParts, ushort fileId, IPEndPoint ipEndPoint)
        {
            _sendBuffer[0] = (byte)CallManagementPacketType.FileManifestResponse;
            // Request ID (uint32)
            _sendBuffer.WriteUint(requestId, 1);
            // Number of Parts (uint16)
            _sendBuffer.WriteUshort(numberOfParts, 5);
            // File ID (uint16)
            _sendBuffer.WriteUshort(fileId, 7);
            _socket.SendTo(_sendBuffer, 0, 9, SocketFlags.None, ipEndPoint);
        }

        readonly Dictionary<uint, ManualResetEvent> _waitingRequests = new Dictionary<uint, ManualResetEvent>();

        async Task<bool> Send(uint requestId, byte[] buffers, int length, IPEndPoint endpoint)
        {
            var manualResetEvent = new ManualResetEvent(false);
            _waitingRequests.Add(requestId, manualResetEvent);
            _socket.SendTo(buffers, 0, length, SocketFlags.None, endpoint);
            bool acknowledged = await Task<bool>.Run(() => manualResetEvent.WaitOne(1000));
            _waitingRequests.Remove(requestId);
            return acknowledged;
        }
    }
}