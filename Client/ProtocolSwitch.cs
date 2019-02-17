using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class ProtocolSwitch
    {
        readonly Socket _socket;
        const int MaxUdpSize = 0x10000;
        const int AnyPort = IPEndPoint.MinPort;
        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        IControlPacketParser _controlPacketParser;
        IMediaPacketParser _mediaPacketParser;
        
        public ProtocolSwitch(ushort startingPort, PortFinder portFinder)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            LocalPort = (ushort)portFinder.BindToAvailablePort(_socket, IPAddress.Any, startingPort);

            Console.WriteLine($"ProtocolSwitch bound to port {LocalPort}");
        }

        public void SetControlPacketParser(IControlPacketParser controlPacketParser)
        {
            _controlPacketParser = controlPacketParser;
        }

        public void SetMediaPacketParser(IMediaPacketParser mediaPacketParser)
        {
            _mediaPacketParser = mediaPacketParser;
        }

        public ushort LocalPort
        {
            get;
        }

        [ThreadStatic]
        static byte[] _sendBuffer;

        public IPEndPoint ServingNodeEndpoint
        {
            get;
            set;
        }

        public byte[] SendBuffer()
        {
            if(_sendBuffer == null)
            {
                _sendBuffer = new byte[MaxUdpSize];
            }
            return _sendBuffer;
        }

        public async Task Run()
        {
            var task = new Task(ProcessPackets, TaskCreationOptions.LongRunning);
            task.Start();
            await task;
        }

        void ProcessPackets()
        {
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
            var packetType = (RopuPacketType)data[0];
            Console.WriteLine($"Received Pakcet {packetType}");
            switch(packetType)
            {
                //Control
                case RopuPacketType.RegistrationResponse:
                    _controlPacketParser?.ParseRegistrationResponse(data);
                    break;
                case RopuPacketType.CallEnded:
                    _controlPacketParser?.ParseCallEnded(data);
                    break;
                case RopuPacketType.CallStartFailed:
                    _controlPacketParser?.ParseCallStartFailed(data);
                    break;
                case RopuPacketType.HeartbeatResponse:
                    _controlPacketParser?.ParseHeartbeatResponse(data);
                    break;
                case RopuPacketType.NotRegistered:
                    _controlPacketParser?.ParseNotRegistered(data);
                    break;
                case RopuPacketType.DeregisterResponse:
                    _controlPacketParser?.ParseDeregisterResponse(data);
                    break;
                //floor
                case RopuPacketType.FloorDenied:
                    throw new NotImplementedException();
                case RopuPacketType.FloorTaken:
                    _controlPacketParser?.ParseFloorTaken(data);
                    break;
                case RopuPacketType.FloorIdle:
                    _controlPacketParser?.ParseFloorIdle(data);
                    break;
                //media
                case RopuPacketType.MediaPacketGroupCallServingNode:
                    _mediaPacketParser?.ParseMediaPacketGroupCall(data);
                    break;
                default:
                    throw new NotSupportedException($"Received unrecognized Packet Type {packetType}");
            }
        }

        public void Send(int length)
        {
            _socket.SendTo(SendBuffer(), 0, length, SocketFlags.None, ServingNodeEndpoint);
        }
    }
}