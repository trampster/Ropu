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
        [ThreadStatic]
        static byte[] _sendBuffer;
        readonly IPEndPoint _remoteEndPoint;
        const int AnyPort = IPEndPoint.MinPort;
        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        readonly Thread _thread;
        readonly int _localPort;
        IControllingFunctionPacketHandler _controllingFunctionHandler;


        public ControllingFunctionClient(int localPort, IPEndPoint remoteEndPoint)
        {
            _localPort = localPort;
            _remoteEndPoint = remoteEndPoint;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, localPort));

            _thread = new Thread(ProcessPackets);
        }

        public void SetControllingFunctionHandler(IControllingFunctionPacketHandler controllingFunctionHandler)
        {
            _controllingFunctionHandler = controllingFunctionHandler;
        }

        static byte[] SendBuffer()
        {
            if(_sendBuffer == null)
            {
                _sendBuffer = new byte[MaxUdpSize];
            }
            return _sendBuffer;
        }

        public void Register(uint userId, ushort rtpPort, ushort floorControlPort)
        {
            var sendBuffer = SendBuffer();
            //packet type (byte)
            sendBuffer[0] = (byte)ControlPacketType.Registration;
            // User ID (uint32)
            sendBuffer.WriteUint(userId, 1);
            // RTP Port (uint16)
            sendBuffer.WriteUshort(rtpPort, 5);
            // Control Plane Port (uint16)
            sendBuffer.WriteUshort((ushort)_localPort, 7);
            // Floor Plane Port (uint16)
            sendBuffer.WriteUshort(floorControlPort, 9);

            _socket.SendTo(sendBuffer, 0, 11, SocketFlags.None, _remoteEndPoint);
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
                    // User ID (uint32), we are going to assume this is correct, to save the clock cycles of varifying it
                    // Codec (byte) (defined via an enum, this is the codec/bitrate used by the system, you must support it, this is required so the server doesn’t have to transcode, which is an expensive operation)
                    Codec codec = (Codec)data[5];
                    // Bitrate (uint16)
                    ushort bitrate = data.Slice(6).ParseUshort();
                    _controllingFunctionHandler?.RegistrationResponseReceived(codec, bitrate);
                    break;
                }
            }
        }
    }
}