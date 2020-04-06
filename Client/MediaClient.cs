using System;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Client.JitterBuffer;
using Ropu.Shared;
using Ropu.Shared.ControlProtocol;

namespace Ropu.Client
{
    public class MediaClient : IMediaClient
    {
        readonly ProtocolSwitch _protocolSwitch;
        readonly IAudioSource _audioSource;
        readonly IAudioPlayer _audioPlayer;
        readonly IAudioCodec _audioCodec;
        readonly IClientSettings _clientSettings;
        readonly IJitterBuffer _jitterBuffer;
        ushort _sequenceNumber = 0;

        public MediaClient(
            ProtocolSwitch protocolSwitch, 
            IAudioSource audioSource,
            IAudioPlayer audioPlayer,
            IAudioCodec audioCodec,
            IJitterBuffer jitterBuffer,
            IClientSettings clientSettings)
        {
            _protocolSwitch = protocolSwitch;
            _protocolSwitch.SetMediaPacketParser(this);
            _audioSource = audioSource;
            _audioPlayer = audioPlayer;
            _audioCodec = audioCodec;
            _jitterBuffer = jitterBuffer;
            _clientSettings = clientSettings;
        }

        uint? _talker;
        public uint? Talker
        {
            set
            {
                _talker = value;
                _jitterBuffer.Talker = value;
            }
        }

        volatile bool _sendingAudio = false;

        public async Task StartSendingAudio(ushort groupId)
        {
            var task = new Task(() =>
            {
                _sendingAudio = true;
                short[] audio = new short[160];
                //bump the sequence number by 1000, this allows the receivers to reset there jitter buffers on new overs.
                //this is required because we don't have a over id in media packets
                _sequenceNumber += 1000; 

                _audioSource.Start();
                while(_sendingAudio)
                {
                    _audioSource.ReadAudio(audio);
                    if(!_sendingAudio)
                    {
                        break; //nothing available
                    }
                    if(_clientSettings.UserId == null) throw new InvalidOperationException("Cannot send audio because no UserId is set");
                    SendMediaPacket(groupId, _sequenceNumber, _clientSettings.UserId.Value, audio);
                    _sequenceNumber++;
                }
                _audioSource.Stop();
            }, TaskCreationOptions.LongRunning);
            task.Start();
            await task;
        }

        public void StopSendingAudio()
        {
            _sendingAudio = false;
        }

        public void ParseMediaPacketGroupCall(Span<byte> data)
        {
            var sequenceNumber = data.Slice(3).ParseUshort();
            var userId = data.Slice(5).ParseUint();
            var audioData = data.Slice(11);
            _jitterBuffer.AddAudio(userId, sequenceNumber, audioData);
        }

        void Silence(short[] buffer)
        {
            for(int index = 0; index < buffer.Length; index++)
            {
                buffer[index] = 0;
            }
        }

        void AudioLoop()
        {
            System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Highest;
            short[] outputBuffer = new short[160];
            
            bool newStream = true;
            Action reset = () =>
            {
                newStream = true;
            };
            while(!_disposing)
            {
                (AudioData? data, bool isNext) = _jitterBuffer.GetNext(reset);

                if(data != null)
                {
                    newStream = false;
                }

                //decode 
                if(newStream)
                {
                    Silence(outputBuffer); //don't do packet loss concellement its just unfilled slots at start of new stream
                }
                else if(data != null || (data == null && _talker != null))
                {
                    _audioCodec.Decode(data, isNext, outputBuffer);
                }
                else
                {
                    Silence(outputBuffer); //don't do packet loss concellement if we don't have a talker
                }

                //play
                _audioPlayer.PlayAudio(outputBuffer);
            }
        }

        public async Task PlayAudio()
        {
            var task = new Task(AudioLoop, TaskCreationOptions.LongRunning);
            task.Start();
            await task;
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

            _protocolSwitch.Send(11 + ammountEncoded);
        }

        protected volatile bool _disposing = false;

        protected void Dispose(bool disposing)
        {
            if(disposing)
            {
                _disposing = true;
                _audioPlayer.Dispose();
                _audioSource.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }
    }
}