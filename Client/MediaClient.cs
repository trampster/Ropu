using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class MediaClient
    {
        readonly ProtocolSwitch _protocolSwitch;

        public MediaClient(ProtocolSwitch protocolSwitch)
        {
            _protocolSwitch = protocolSwitch;
        }

        public void SendMediaPacket(ushort callId, byte[] payload, IPEndPoint endPoint)
        {
            var sendBuffer = _protocolSwitch.SendBuffer();
            int length = BuildMediaPacket(1234, payload, sendBuffer);
            _protocolSwitch.Send(length, endPoint);
        }

        int BuildMediaPacket(ushort callId, byte[] payload, byte[] buffer)
        {
            buffer[0] = (byte)CombinedPacketType.Media;
            buffer.WriteUshort(callId, 1);
            buffer.WriteUshort(0, 3);
            buffer.AsSpan(5).WriteArray(buffer);

            int bufferIndex = 4;
            for(int payloadIndex = 0; payloadIndex < payload.Length; payloadIndex++)
            {
                buffer[bufferIndex] = payload[payloadIndex];
                bufferIndex++;
            }
            return bufferIndex;
        }
    }
}