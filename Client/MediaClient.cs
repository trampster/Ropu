using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public interface IAudioSource
    {
        void Start();

        void Stop();

        /// <summary>
        /// Read 20 ms of audio at 8000 samples/s (160 samples or 320 bytes)
        /// Should block until buffer is filled or source is stopped
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        void ReadAudio(byte[] buffer);

    }

    public class MediaClient : IMediaPacketParser
    {
        readonly ProtocolSwitch _protocolSwitch;
        readonly IAudioSource _audioSource;
        readonly IClientSettings _clientSettings;
        ushort _sequenceNumber = 0;

        public MediaClient(ProtocolSwitch protocolSwitch, IAudioSource audioSource, IClientSettings clientSettings)
        {
            _protocolSwitch = protocolSwitch;
            _protocolSwitch.SetMediaPacketParser(this);
            _audioSource = audioSource;
            _clientSettings = clientSettings;
        }

        bool _sendingAudio = false;

        async Task StartSendingAudio(ushort groupId)
        {
            byte[] buffer = new byte[320];
            while(_sendingAudio)
            {
                await Task.Run(() => _audioSource.ReadAudio(buffer));
                if(!_sendingAudio)
                {
                    return; //nothing available
                }
                SendMediaPacket(groupId, _sequenceNumber, _clientSettings.UserId, buffer);
                _sequenceNumber++;
            }
        }

        void StopSendingAudio()
        {
            _audioSource.Stop();
        }



        public void ParseMediaPacketGroupCall(Span<byte> data)
        {
            throw new NotImplementedException();
        }

        void SendMediaPacket(ushort groupId, ushort sequenceNumber, uint userId, byte[] payload)
        {
            var buffer = _protocolSwitch.SendBuffer();

            // Packet Type 12 (byte)
            buffer[0] = (byte)RopuPacketType.MediaPacketGroupCall;
            // Group Id (uint16)
            buffer.WriteUshort(groupId, 1);
            // Sequence Number (uint16)
            buffer.WriteUshort(sequenceNumber, 3);
            // User ID (uint32)
            buffer.WriteUint(sequenceNumber, 5);
            // Key ID (uint16) - 0 means no encryption
            buffer.WriteUshort(0, 9);
            // Payload
            buffer.AsSpan(11).WriteArray(payload);

            _protocolSwitch.Send(11 + buffer.Length);
        }
    }
}