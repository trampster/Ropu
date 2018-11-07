using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.LoadBalancer
{
    public class ControlProtocol
    {
        readonly Socket _socket;
        readonly int _port;
        const int AnyPort = IPEndPoint.MinPort;
        const int MaxUdpSize = 0x10000;

        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        IControlMessageHandler _messageHandler;

        public ControlProtocol(int port)
        {
            _port = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void SetMessageHandler(IControlMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
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
                HandlePacket(receivedBytes, (IPEndPoint)any);
            }
        }

        void HandlePacket(Span<byte> data, IPEndPoint endPoint)
        {
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

        readonly byte[] _sendBuffer = new byte[MaxUdpSize];

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

        public void SendCallStarted(uint userId, ushort groupId, ushort callId, IPEndPoint mediaEndpoint, IPEndPoint floorControlEndpoint, IEnumerable<IPEndPoint> endPoints)
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