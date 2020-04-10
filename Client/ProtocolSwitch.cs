using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class ProtocolSwitch
    {
        readonly Socket _socket;
        readonly PacketEncryption _packetEncryption;
        readonly KeysClient _keysClient;
        const int MaxUdpSize = 0x10000;
        const int AnyPort = IPEndPoint.MinPort;
        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        IControlPacketParser? _controlPacketParser;
        IMediaPacketParser? _mediaPacketParser;
        IClientSettings _clientSettings;
        
        public ProtocolSwitch(
            ushort startingPort, 
            IPortFinder portFinder, 
            PacketEncryption packetEncryption, 
            KeysClient keysClient,
            IClientSettings clientSettings)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _packetEncryption = packetEncryption;
            _keysClient  = keysClient;
            _clientSettings = clientSettings;
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

        static ThreadLocal<byte[]> _sendBuffer = new ThreadLocal<byte[]>(() => new byte[MaxUdpSize]);
        static ThreadLocal<byte[]> _sendBufferEncrypted = new ThreadLocal<byte[]>(() => new byte[MaxUdpSize]);

        public IPEndPoint? ServingNodeEndpoint
        {
            get;
            set;
        }

        public byte[] SendBuffer()
        {
            #nullable disable
            return _sendBuffer.Value;
            #nullable enable
        }

        byte[] SendBufferEncrypted()
        {
            #nullable disable
            return _sendBufferEncrypted.Value;
            #nullable enable
        }

        public async Task Run()
        {
            var task = new Task(async () => await ProcessPackets(), TaskCreationOptions.LongRunning);
            task.Start();
            await task;
        }

        async Task ProcessPackets()
        {
            byte[] _buffer = new byte[MaxUdpSize];
            byte[] payload = new byte[MaxUdpSize];
            EndPoint any = Any;

            while(true)
            {
                int ammountRead = _socket.ReceiveFrom(_buffer, ref any);
                int payloadLength;
                try
                {
                    payloadLength = await _packetEncryption.Decrypt(_buffer, ammountRead, payload);
                }
                catch(Exception exception)
                {
                    Console.Error.WriteLine($"Failed to decrypt packet with Exception {exception}");
                    continue;
                }

                HandlePacket(payload.AsSpan(0, payloadLength), ((IPEndPoint)any).Address);
            }
        }

        void HandlePacket(Span<byte> data, IPAddress ipaddress)
        {
            var packetType = (RopuPacketType)data[0];
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
                    Console.Error.WriteLine("Got Floor Denied but not Implemented");
                    break;
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
            //_socket.SendTo(SendBuffer(), 0, length, SocketFlags.None, ServingNodeEndpoint);
            if(ServingNodeEndpoint == null)
            {
                throw new InvalidOperationException("Can't send packet to serving node because we don't have a serving node yet");
            }

            SendToEncrypted(SendBuffer(), length, ServingNodeEndpoint);
        }

        bool SendToEncrypted(byte[] buffer, int length, IPEndPoint endPoint)
        {
            var userId = _clientSettings.UserId;
            var keyInfo = _keysClient.GetMyKeyInfo();
            if(userId == null || keyInfo ==null)
            {
                return false;
            }
            var packet =  SendBufferEncrypted();
            int packetLength = _packetEncryption.CreateEncryptedPacket(buffer.AsSpan(0,  length), packet, false, userId.Value, keyInfo);
            _socket.SendTo(packet, 0, packetLength, SocketFlags.None, endPoint);
            return true;
        }
    }
}