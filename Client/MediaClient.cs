using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class MediaClient : IMediaPacketParser
    {
        readonly ProtocolSwitch _protocolSwitch;
        readonly IAudioSource _audioSource;
        readonly IAudioCodec _audioCodec;
        readonly IClientSettings _clientSettings;
        ushort _sequenceNumber = 0;

        public MediaClient(ProtocolSwitch protocolSwitch, IAudioSource audioSource, IAudioCodec audioCodec, IClientSettings clientSettings)
        {
            _protocolSwitch = protocolSwitch;
            _protocolSwitch.SetMediaPacketParser(this);
            _audioSource = audioSource;
            _audioCodec = audioCodec;
            _clientSettings = clientSettings;
        }

        volatile bool _sendingAudio = false;

        public async Task StartSendingAudio(ushort groupId)
        {
            _sendingAudio = true;
            short[] audio = new short[160];
            while(_sendingAudio)
            {
                await Task.Run(() => _audioSource.ReadAudio(audio));
                if(!_sendingAudio)
                {
                    return; //nothing available
                }
                SendMediaPacket(groupId, _sequenceNumber, _clientSettings.UserId, audio);
                _sequenceNumber++;
            }
        }

        public void StopSendingAudio()
        {
            _sendingAudio = false;
        }


        public void ParseMediaPacketGroupCall(Span<byte> data)
        {
            Console.WriteLine("Received media packet");
        }

        void SendMediaPacket(ushort groupId, ushort sequenceNumber, uint userId, short[] audio)
        {
            var buffer = _protocolSwitch.SendBuffer();

            // Packet Type 12 (byte)
            buffer[0] = (byte)RopuPacketType.MediaPacketGroupCallClient;
            // Group Id (uint16)
            buffer.WriteUshort(groupId, 1);
            // Sequence Number (uint16)
            buffer.WriteUshort(sequenceNumber, 3);
            // User ID (uint32)
            buffer.WriteUint(userId, 5);
            // Key ID (uint16) - 0 means no encryption
            buffer.WriteUshort(0, 9);
            // Payload
            int ammountEncoded = _audioCodec.Encode(audio, buffer.AsSpan(11));

            Console.WriteLine("Sending Media Packet");
            _protocolSwitch.Send(11 + ammountEncoded);
        }
    }
}