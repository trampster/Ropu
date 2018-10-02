using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.ContollingFunction
{
    public class ControlProtocol
    {
        readonly Registra _registra;
        readonly Socket _socket;
        readonly int _port;
        const int AnyPort = IPEndPoint.MinPort;
        const int MaxUdpSize = 0x10000;

        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);

        public ControlProtocol(Registra registra, int port)
        {
            _port = port;
            _registra = registra;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void ProcessPackets()
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

        uint ParseUint(Span<byte> data)
        {
            return (uint)(
                (data[0] << 24) +
                (data[1] << 16) +
                (data[2] << 8) +
                data[3]); 
        }

        ushort ParseUshort(Span<byte> data)
        {
            return (ushort)(
                (data[0] << 8) +
                (data[1])); 
        }

        void HandlePacket(Span<byte> data, IPAddress ipaddress)
        {
            switch((ControlPacketType)data[0])
            {
                case ControlPacketType.Registration:
                {
                    uint userId = ParseUint(data.Slice(1));
                    ushort rtpPort = ParseUshort(data.Slice(5));
                    ushort controlPlanePort = ParseUshort(data.Slice(7));
                    ushort floorControlPort = ParseUshort(data.Slice(9));
                    var registration = new Registration(userId, rtpPort, floorControlPort, new IPEndPoint(ipaddress, controlPlanePort));
                    _registra.Register(registration);
                    SendRegisterResponse(registration);
                    break;
                }
            }
        }

        readonly byte[] _sendBuffer = new byte[MaxUdpSize];

        void WriteUint(byte[] buffer, uint value, int start)
        {
            buffer[start]     = (byte)((value & 0xFF000000) >> 24);
            buffer[start + 1] = (byte)((value & 0x00FF0000) >> 16);
            buffer[start + 2] = (byte)((value & 0x0000FF00) >> 8);
            buffer[start + 3] = (byte) (value & 0x000000FF);
        }

        void WriteUshort(byte[] buffer, uint value, int start)
        {
            buffer[start]     = (byte)((value & 0x0000FF00) >> 8);
            buffer[start + 1] = (byte) (value & 0x000000FF);
        }

        void SendRegisterResponse(Registration registration)
        {
            // Packet Type 1
            _sendBuffer[0] = (byte)ControlPacketType.RegistrationResponse;
            // User ID (uint32)
            _sendBuffer.WriteUint(registration.UserId, 1);
            // Codec (byte) (defined via an enum, this is the codec/bitrate used by the system, you must support it, this is required so the server doesn’t have to transcode, which is an expensive operation)
            _sendBuffer[7] = (byte)Codecs.Opus;
            // Bitrate (uint16)
            _sendBuffer.WriteUshort(8000, 8);

            Console.WriteLine($"Sending registration response to {registration.ControlEndpoint}");
            _socket.SendTo(_sendBuffer, 0, 10, SocketFlags.None, registration.ControlEndpoint);
        }
    }
}