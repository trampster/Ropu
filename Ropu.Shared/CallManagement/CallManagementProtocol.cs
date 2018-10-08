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
        readonly int _port;
        const int AnyPort = IPEndPoint.MinPort;
        const int MaxUdpSize = 0x10000;
        readonly byte[] _sendBuffer = new byte[MaxUdpSize];
        uint _requestId = 0;


        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        ICallManagementServerMessageHandler _serverMessageHandler;
        ICallManagementClientMessageHandler _clientMessageHandler;

        public CallManagementProtocol(int port)
        {
            _port = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

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
            _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
            byte[] _buffer = new byte[MaxUdpSize];
            EndPoint any = Any;

            while(true)
            {
                int ammountRead = _socket.ReceiveFrom(_buffer, ref any);

                var receivedBytes = new Span<byte>(_buffer, 0, ammountRead);
                HandlePacket(receivedBytes, ((IPEndPoint)any).Address);
            }
        }

        void HandlePacket(Span<byte> data, IPAddress ipaddress)
        {
            switch((CallManagementPacketType)data[0])
            {
                case CallManagementPacketType.RegisterMediaController:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort controlPort = data.Slice(5).ParseUshort();
                    var mediaEndpoint = data.Slice(7).ParseIPEndPoint();
                    _serverMessageHandler?.HandleRegisterMediaController(ipaddress, requestId, controlPort, mediaEndpoint);
                    break;
                }
                case CallManagementPacketType.RegisterFloorController:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort controlPort = data.Slice(5).ParseUshort();
                    var floorControlEndpoint = data.Slice(7).ParseIPEndPoint();
                    _serverMessageHandler?.HandleRegisterFloorController(ipaddress, requestId, controlPort, floorControlEndpoint);
                    break;
                }
                case CallManagementPacketType.StartCall:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    ushort callId = data.Slice(5).ParseUshort();
                    ushort groupId = data.Slice(7).ParseUshort();
                    _clientMessageHandler?.CallStart(requestId, callId, groupId);
                    break;
                }
                case CallManagementPacketType.Ack:
                {
                    uint requestId = data.Slice(1).ParseUint();
                    HandleAck(requestId);
                    break;
                }
            }
        }

        void HandleAck(uint requestId)
        {
            if(_waitingRequests.TryGetValue(requestId, out ManualResetEvent resetEvent))
            {
                resetEvent.Set();
            }
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
            _sendBuffer[0] = (byte)CallManagementPacketType.StartCall;
            //Request ID (uint32)
            _sendBuffer.WriteUint(requestId, 1);
            _socket.SendTo(_sendBuffer, 0, 5, SocketFlags.None, ipEndPoint);
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