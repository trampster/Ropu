using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ropu.ContollingFunction
{
    public class ControlProtocol
    {
        readonly Registra _registra;
        readonly Socket _socket;
        const int AnyPort = IPEndPoint.MinPort;
        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);

        public ControlProtocol(Registra registra)
        {
            _registra = registra;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        }

        public void ProcessPackets()
        {

            const int MaxUDPSize = 0x10000;
            byte[] _buffer = new byte[MaxUDPSize];
            EndPoint any = Any;

            while(true)
            {
                int ammountRead = _socket.ReceiveFrom(_buffer, ref any);

                var receivedBytes = new Span<byte>(_buffer, 0, ammountRead);
                HandlePacket(receivedBytes);
            }
        }

        uint ParseUint(Span<byte> data)
        {
            return (uint)(
                (data[0] << 3) +
                (data[1] << 2) +
                (data[2] << 1) +
                data[3]); 
        }

        ushort ParseUshort(Span<byte> data)
        {
            return (ushort)(
                (data[0] << 1) +
                (data[1])); 
        }

        void HandlePacket(Span<byte> data)
        {
            switch((ControlPacketType)data[0])
            {
                case ControlPacketType.Registration:
                {
                    uint userId = ParseUint(data.Slice(1));
                    ushort rtpPort = ParseUshort(data.Slice(5));
                    ushort controlPlanePort = ParseUshort(data.Slice(7));
                    var registration = new Registration(userId, rtpPort, controlPlanePort);
                    _registra.Register(registration);
                    
                    break;
                }
            }
        }

        void SendRegisterResponse(Registration registration)
        {
        }
    }
}