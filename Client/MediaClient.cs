using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class MediaClient : IMediaPacketParser
    {
        readonly ProtocolSwitch _protocolSwitch;

        public MediaClient(ProtocolSwitch protocolSwitch)
        {
            _protocolSwitch = protocolSwitch;
            _protocolSwitch.SetMediaPacketParser(this);
        }

        public void ParseMediaPacketGroupCall(Span<byte> data)
        {
            throw new NotImplementedException();
        }

        public void SendMediaPacket(ushort callId, byte[] payload, IPEndPoint endPoint)
        {
            var sendBuffer = _protocolSwitch.SendBuffer();
            int length = BuildMediaPacket(1234, payload, sendBuffer);
            _protocolSwitch.Send(length, endPoint);
        }

        int BuildMediaPacket(ushort callId, byte[] payload, byte[] buffer)
        {
            buffer[0] = (byte)RopuPacketType.MediaPacketGroupCall;
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