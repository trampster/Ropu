using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.AsyncTools;
using Ropu.Shared.ControlProtocol;

namespace Ropu.ServingNode
{
    public class MediaProtocol
    {
        IPEndPoint[] _endpoints;
        readonly Socket _socket;
        const int MaxUdpSize = 0x10000;
        const int AnyPort = IPEndPoint.MinPort;
        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        readonly byte[] _receiveBuffer = new byte[MaxUdpSize];

        IMessageHandler _messageHandler;

        readonly byte[] _sendBuffer = new byte[MaxUdpSize];

        public MediaProtocol(PortFinder portFinder, int startingPort)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            MediaPort = (ushort)portFinder.BindToAvailablePort(_socket, IPAddress.Any, startingPort);
            Console.WriteLine($"Serving Node Protocol bound to port {MediaPort}");

            //setup dummy group endpoints
            _endpoints = new IPEndPoint[10000];
            for(int endpointIndex = 0; endpointIndex < _endpoints.Length; endpointIndex++)
            {
                _endpoints[endpointIndex] = new IPEndPoint(IPAddress.Parse("192.168.1.2"), endpointIndex + 1000);
            }
        }

        public void SetMessageHandler(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
        }

        public ushort MediaPort
        {
            get;
        }

        public async Task Run()
        {
            var task = new Task(() => AsyncPump.Run(ProcessPackets), TaskCreationOptions.LongRunning);
            task.Start();
            await task;
        }

        async Task ProcessPackets()
        {

            byte[] buffer = new byte[MaxUdpSize];
            var segment = new ArraySegment<byte>(buffer);
            EndPoint any = Any;

            var resetEvent = new ManualResetEvent(false);

            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(buffer);
            socketArgs.RemoteEndPoint = any;
            socketArgs.SocketFlags = SocketFlags.None;
            socketArgs.Completed += (sender, args) =>
            {
                resetEvent.Set();
            };

            while(true)
            {
                if(_socket.ReceiveFromAsync(socketArgs))
                {
                    //didn't complete yet, need to wait for it
                    await Task.Run(() => resetEvent.WaitOne());
                    resetEvent.Reset();
                }

                HandlePacket(buffer, socketArgs.BytesTransferred, (IPEndPoint)socketArgs.RemoteEndPoint);
            }
        }

        void HandlePacket(byte[] buffer, int ammountRead, IPEndPoint endPoint)
        {
            var data = new Span<byte>(buffer, 0, ammountRead);

            switch((CombinedPacketType)data[0])
            {
                case CombinedPacketType.Registration:
                {
                    uint userId = data.Slice(1).ParseUint();
                    _messageHandler.Registration(userId, endPoint);
                    break;
                }
                case CombinedPacketType.StartGroupCall:
                {
                    // User ID (uint32)
                    uint userId = data.Slice(1).ParseUint();
                    // Group ID (uint16)
                    ushort groupId = data.Slice(5).ParseUshort();
                    _messageHandler.StartGroupCall(userId, groupId, endPoint);
                    break;
                }
            }
        }

        void BulkSendAsync(byte[] buffer, int length, Socket socket)
        {
            for(int endpointIndex = 0; endpointIndex < _endpoints.Length; endpointIndex++)
            {
                var args = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = _endpoints[endpointIndex],
                };
                args.SetBuffer(buffer, 0, length);

                socket.SendAsync(args);
            }
        }

        void BulkSendParallelFor(byte[] buffer, int length, Socket socket)
        {
            Parallel.For(0, _endpoints.Length, new ParallelOptions(){MaxDegreeOfParallelism = 40}, endpointIndex => 
                socket.SendTo(buffer, 0, length, SocketFlags.None, _endpoints[endpointIndex]));
        }

        void BulkSendSync(byte[] buffer, int length, Socket socket)
        {
            for(int endpointIndex = 0; endpointIndex < _endpoints.Length; endpointIndex++)
            {
                socket.SendTo(buffer, 0, length, SocketFlags.None, _endpoints[endpointIndex]);
            }
        }

        public void SendRegisterResponse(Registration registration, IPEndPoint endPoint)
        {
            // Packet Type
            _sendBuffer[0] = (byte)CombinedPacketType.RegistrationResponse;
            // User ID (uint32)
            _sendBuffer.WriteUint(registration.UserId, 1);
            // Codec (byte) (defined via an enum, this is the codec/bitrate used by the system, you must support it, 
            // this is required so the server doesn’t have to transcode, which is an expensive operation)
            _sendBuffer[7] = (byte)Codecs.Opus;
            // Bitrate (uint16)
            _sendBuffer.WriteUshort(8000, 8);

            Console.WriteLine($"Sending registration response to {endPoint}");
            _socket.SendTo(_sendBuffer, 0, 10, SocketFlags.None, endPoint);
        }

        public void SendCallStarted(
            uint userId, ushort groupId, ushort callId, 
            IPEndPoint mediaEndpoint, IPEndPoint floorControlEndpoint, 
            IEnumerable<IPEndPoint> endPoints)
        {
            // Packet Type
            _sendBuffer[0] = (byte)CombinedPacketType.CallStarted;
            // User Id (uint32)
            _sendBuffer.WriteUint(userId, 1);
            // Group ID (uint16)
            _sendBuffer.WriteUshort(groupId, 5);
            // Call ID (uint16) unique identifier for the call, to be included in the media stream
            _sendBuffer.WriteUshort(callId, 7);
            // Media Endpoint (4 bytes IP Address, 2 bytes port)
            _sendBuffer.WriteEndPoint(mediaEndpoint, 9);
            // Floor Control Endpoint (4 bytes IP Address, 2 bytes port)
            _sendBuffer.WriteEndPoint(mediaEndpoint, 15);

            foreach(var endpoint in endPoints)
            {
                _socket.SendTo(_sendBuffer, 0, 21, SocketFlags.None, endpoint);
            }
        }

        public void SendCallStartFailed(CallFailedReason reason, uint userId, IPEndPoint endPoint)
        {
            // Packet Type
            _sendBuffer[0] = (byte)CombinedPacketType.CallStartFailed;
            // User ID (uint32) 
            _sendBuffer.WriteUint(userId, 1);
            //* Reason (byte) 0 = insufficient resources, 255 = other reason
            _sendBuffer[5] = (byte)CallFailedReason.InsufficientResources;

            _socket.SendTo(_sendBuffer, 0, 6, SocketFlags.None, endPoint);
        }
    }
}