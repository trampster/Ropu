using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class MediaClient
    {
        readonly Socket _socket;
        const int MaxUDPSize = 0x10000;
        readonly byte[] _sendBuffer = new byte[MaxUDPSize];
        readonly IPEndPoint _remoteEndPoint;

        MediaClient(int localPort, IPEndPoint remoteEndPoint)
        {
            _remoteEndPoint = remoteEndPoint;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
        }

        public void SendMediaPakcet(ushort callId, byte[] payload, byte[] buffer)
        {
            int length = BuildMediaPacket(1234, payload, buffer);
            _socket.SendTo(buffer, 0, length, SocketFlags.None, _remoteEndPoint);
        }

        int BuildMediaPacket(ushort callId, byte[] payload, byte[] buffer)
        {
            buffer[0] = (byte)((callId & 0xFF00) >> 8);
            buffer[1] = (byte)(callId & 0xFF);

            int bufferIndex = 2;
            for(int payloadIndex = 0; payloadIndex < payload.Length; payloadIndex++)
            {
                buffer[bufferIndex] = payload[payloadIndex];
                bufferIndex++;
            }
            return bufferIndex;
        }
    }
}