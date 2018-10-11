using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
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
        
        public ProtocolSwitch(ushort port)
        {
            LocalPort = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public void SetControlPacketParser(IControlPacketParser controlPacketParser)
        {
            _controlPacketParser = controlPacketParser;
        }

        public ushort LocalPort
        {
            get;
        }

        [ThreadStatic]
        static byte[] _sendBuffer;

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
            var packetType = (CombinedPacketType)data[0];
            switch((CombinedPacketType)data[0])
            {
                //Control
                case CombinedPacketType.RegistrationResponse:
                    _controlPacketParser?.ParseRegistrationResponse(data);
                    break;
                case CombinedPacketType.CallEnded:
                    _controlPacketParser?.ParseCallEnded(data);
                    break;
                case CombinedPacketType.CallStarted:
                    _controlPacketParser?.ParseCallStarted(data);
                    break;
                case CombinedPacketType.CallStartFailed:
                    _controlPacketParser?.ParseCallStartFailed(data);
                    break;
                //floor
                case CombinedPacketType.FloorDenied:
                    throw new NotImplementedException();
                case CombinedPacketType.FloorGranted:
                    throw new NotImplementedException();
                case CombinedPacketType.FloorReleased:
                    throw new NotImplementedException();
                //media
                case CombinedPacketType.Media:
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException($"Received unrecognized Packet Type {packetType}");
            }
        }

        public void Send(int length, IPEndPoint endpoint)
        {
            _socket.SendTo(SendBuffer(), 0, length, SocketFlags.None, endpoint);
        }
    }
}