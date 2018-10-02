using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class ControllingFunctionClient
    {
        readonly Socket _socket;
        const int MaxUdpSize = 0x10000;
        readonly byte[] _sendBuffer = new byte[MaxUdpSize];
        readonly IPEndPoint _remoteEndPoint;
        const int AnyPort = IPEndPoint.MinPort;
        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        readonly Thread _thread;


        public ControllingFunctionClient(int localPort, IPEndPoint remoteEndPoint)
        {
            _remoteEndPoint = remoteEndPoint;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, localPort));

            _thread = new Thread(ProcessPackets);
        }

        public void Register(uint userId, ushort rtpPort, ushort controlPort, ushort floorControlPort)
        {
            //packet type (byte)
            _sendBuffer[0] = (byte)ControlPacketType.Registration;
            // User ID (uint32)
            _sendBuffer.WriteUint(userId, 1);
            // RTP Port (uint16)
            _sendBuffer.WriteUshort(rtpPort, 5);
            // Control Plane Port (uint16)
            _sendBuffer.WriteUshort(controlPort, 7);
            // Floor Plane Port (uint16)
            _sendBuffer.WriteUshort(floorControlPort, 9);

            _socket.SendTo(_sendBuffer, 0, 11, SocketFlags.None, _remoteEndPoint);
        }

        public void StartListening()
        {
            _thread.Start();
        }

        public void ProcessPackets()
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
            switch((ControlPacketType)data[0])
            {
                case ControlPacketType.RegistrationResponse:
                {
                    Console.WriteLine("Got Registration Response");
                    break;
                }
            }
        }
    }
}