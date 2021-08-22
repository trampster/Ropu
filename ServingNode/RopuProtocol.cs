using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.AsyncTools;
using Ropu.Shared.ControlProtocol;

namespace Ropu.ServingNode
{
    public class RopuProtocol
    {
        readonly Socket _socket;
        const int MaxUdpSize = 0x10000;
        const int AnyPort = IPEndPoint.MinPort;
        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        readonly byte[] _receiveBuffer = new byte[MaxUdpSize];

        IMessageHandler? _messageHandler = null;

        readonly byte[] _sendBuffer = new byte[MaxUdpSize];
        readonly byte[] _encryptedBuffer = new byte[MaxUdpSize];

        readonly PacketEncryption _packetEncryption;
        readonly KeysClient _keysClient;

        public RopuProtocol(PortFinder portFinder, int startingPort, PacketEncryption packetEncryption, KeysClient keysClient)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            MediaPort = (ushort)portFinder.BindToAvailablePort(_socket, IPAddress.Any, startingPort);
            Console.WriteLine($"Serving Node Protocol bound to port {MediaPort}");

            _socketEventArgsPool = new MemoryPool<SocketAsyncEventArgs>(() => CreateSocketAsyncEventArgs());
            _bulkRequestTokenPool = new MemoryPool<BulkRequestToken>(() => new BulkRequestToken(ReleaseBulkRequestToken));

            _packetEncryption = packetEncryption;
            _keysClient = keysClient;
        }

        void ReleaseBulkRequestToken(BulkRequestToken token)
        {
            _bulkRequestTokenPool.Add(token);
        }

        public void SetMessageHandler(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
        }

        public ushort MediaPort
        {
            get;
        }

        public async Task Run()
        {
            var task = new Task(() => AsyncPump.Run(ProcessPackets), TaskCreationOptions.LongRunning);
            task.Start();
            await task;
        }

        async Task ProcessPackets()
        {

            byte[] buffer = new byte[MaxUdpSize];
            byte[] payload = new byte[MaxUdpSize];
            var segment = new ArraySegment<byte>(buffer);
            EndPoint any = Any;

            var resetEvent = new ManualResetEvent(false);

            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(buffer);
            socketArgs.RemoteEndPoint = any;
            socketArgs.SocketFlags = SocketFlags.None;
            socketArgs.Completed += (sender, args) =>
            {
                resetEvent.Set();
            };

            while(true)
            {
                if(_socket.ReceiveFromAsync(socketArgs))
                {
                    //didn't complete yet, need to wait for it
                    await Task.Run(() => resetEvent.WaitOne());
                    resetEvent.Reset();
                }

                int payloadLength = await _packetEncryption.Decrypt(buffer, socketArgs.BytesTransferred, payload);
                
                if(payloadLength != 0)
                {
                    HandlePacket(payload, payloadLength, (IPEndPoint)socketArgs.RemoteEndPoint);
                }
            }
        }

        void HandlePacket(byte[] buffer, int ammountRead, IPEndPoint endPoint)
        {
            var data = new Span<byte>(buffer, 0, ammountRead);

            switch((RopuPacketType)data[0])
            {
                case RopuPacketType.Registration:
                {
                    uint userId = data.Slice(1).ParseUint();
                    _messageHandler?.Registration(userId, endPoint);
                    break;
                }
                case RopuPacketType.Deregister:
                {
                    uint userId = data.Slice(1).ParseUint();
                    _messageHandler?.Deregister(userId, endPoint);
                    break;
                }
                case RopuPacketType.Heartbeat:
                {
                    uint userId = data.Slice(1).ParseUint();
                    _messageHandler?.Heartbeat(userId, endPoint);
                    break;
                }
                case RopuPacketType.StartGroupCall:
                case RopuPacketType.FloorReleased:
                {
                    Console.WriteLine("Got Start Group Call Message");
                    ushort groupId = data.Slice(1).ParseUshort();
                    _messageHandler?.HandleCallControllerMessage(groupId, buffer, ammountRead);
                    break;
                }
                case RopuPacketType.FloorRequest:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    _messageHandler?.HandleCallControllerMessage(groupId, buffer, ammountRead);
                    break;
                }
                case RopuPacketType.CallEnded:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    _messageHandler?.HandleCallEnded(groupId, buffer, ammountRead, endPoint);
                    break;
                }
                case RopuPacketType.FloorTaken:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    _messageHandler?.ForwardFloorTaken(groupId, buffer, ammountRead, endPoint);
                    break;
                }
                case RopuPacketType.FloorIdle:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    _messageHandler?.ForwardFloorIdle(groupId, buffer, ammountRead, endPoint);
                    break;
                }
                case RopuPacketType.MediaPacketGroupCallClient:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    _messageHandler?.ForwardClientMediaPacket(groupId, buffer, ammountRead, endPoint);
                    break;
                }
                case RopuPacketType.MediaPacketGroupCallServingNode:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    _messageHandler?.ForwardPacketToClients(groupId, buffer, ammountRead, endPoint);
                    break;
                }
                default:
                {
                    Console.Error.WriteLine($"Received unknown packet, packet type {data[0]}");
                    break;
                }
            }
        }

        public void SendGroupPacket(byte[] packet, int length, IPEndPoint target, uint groupId, CachedEncryptionKey keyInfo)
        {
            int packetLength = _packetEncryption.CreateEncryptedPacket(packet.AsSpan(0, length), _encryptedBuffer, true, groupId, keyInfo);

            _socket.SendTo(_encryptedBuffer, 0, packetLength, SocketFlags.None, target);
        }

        void SendToUserEncrypted(Span<byte> payload, IPEndPoint endPoint)
        {
            var keyInfo = _keysClient.GetMyKeyInfo();
            if(keyInfo == null)
            {
                Console.Error.WriteLine("Failed to send encrypted packet because we could not get our key info");
                return;
            }
            int packetLength = _packetEncryption.CreateEncryptedPacket(payload, _encryptedBuffer, false, UserId, keyInfo);

            _socket.SendTo(_encryptedBuffer, 0, packetLength, SocketFlags.None, endPoint);
        }

        readonly MemoryPool<SocketAsyncEventArgs> _socketEventArgsPool;
        readonly MemoryPool<BulkRequestToken> _bulkRequestTokenPool;
        readonly object _asyncCompleteLock = new object();

        SocketAsyncEventArgs CreateSocketAsyncEventArgs()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += AsyncSendComplete;
            return args;
        }

        class BulkRequestToken
        {
            int _waitingSendCount = 0;
            readonly Action<BulkRequestToken> _release;

            public BulkRequestToken(Action<BulkRequestToken> release)
            {
                _release = release;
            }

            public void Reset()
            {
                Finished = null;
                _waitingSendCount = 0;
            }

            public int WaitingCount => _waitingSendCount;

            public int IncrementWaitingCount()
            {
                return Interlocked.Increment(ref _waitingSendCount);
            }

            public int DecrementWaitingCount()
            {
                return Interlocked.Decrement(ref _waitingSendCount);
            }

            public Action<object?>? Finished
            {
                get;
                set;
            }

            public object? State
            {
                get;
                set;
            }

            public void Finish()
            {
                Finished?.Invoke(State);
                _release(this);
            }
        }

        void AsyncSendComplete(object? sender, SocketAsyncEventArgs args)
        {
            _socketEventArgsPool.Add(args);

            lock(_asyncCompleteLock)
            {
                var token = (BulkRequestToken)args.UserToken!;
                int newCount = token.DecrementWaitingCount();
                if(newCount == 0)
                {
                    token.Finish();
                }
            }
        }

        public void BulkSendAsync(byte[] buffer, int length, Span<IPEndPoint> endPoints, Action<object?> onComplete, object state, IPEndPoint except, uint groupId, CachedEncryptionKey keyInfo)
        {
            var token = _bulkRequestTokenPool.Get();
            token.Reset();
            token.Finished = onComplete;
            token.State = state;

            for(int endpointIndex = 0; endpointIndex < endPoints.Length; endpointIndex++)
            {
                var args = _socketEventArgsPool.Get();
                args.UserToken = token;
                var endPoint = endPoints[endpointIndex];
                if(endPoint.Equals(except))
                {
                    continue;
                }
                args.RemoteEndPoint = endPoint;
                SetBufferEncrypted(args, buffer, length, groupId, keyInfo);

                if(!_socket.SendToAsync(args))
                {
                    //completed syncronously
                    _socketEventArgsPool.Add(args); //release the memory back to the pool
                }
                else
                {
                    token.IncrementWaitingCount();
                }
            }
            lock(_asyncCompleteLock)
            {
                if(token.WaitingCount == 0) 
                {
                    token.Finish();
                    return;
                }
            }
        }

        public void SendDeregisterResponse(IPEndPoint endPoint)
        {
            // Packet Type
            _sendBuffer[0] = (byte)RopuPacketType.DeregisterResponse;
            
            SendToUserEncrypted(_sendBuffer.AsSpan(0,1), endPoint);
        }

        public uint UserId
        {
            get;
            set;
        }

        public void SendNotRegistered(IPEndPoint endPoint)
        {
            // Packet Type
            _sendBuffer[0] = (byte)RopuPacketType.NotRegistered;
            SendToUserEncrypted(_sendBuffer.AsSpan(0,1), endPoint);
        }

        public void SendHeartbeatResponse(IPEndPoint endPoint)
        {
             // Packet Type
            _sendBuffer[0] = (byte)RopuPacketType.HeartbeatResponse;
            SendToUserEncrypted(_sendBuffer.AsSpan(0,1), endPoint);
        }

        void SetBufferEncrypted(SocketAsyncEventArgs args, byte[] buffer, int length, uint groupId, CachedEncryptionKey keyInfo)
        {
            int packetLength = _packetEncryption.CreateEncryptedPacket(buffer.AsSpan(0, length), _encryptedBuffer, true, groupId, keyInfo);
            args.SetBuffer(_encryptedBuffer, 0, packetLength);
        }

        public void BulkSendAsync(byte[] buffer, int length, Span<IPEndPoint> endPoints, Action<object?> onComplete, object state, uint groupId, CachedEncryptionKey keyInfo)
        {
            var token = _bulkRequestTokenPool.Get();
            token.Reset();
            token.Finished = onComplete;
            token.State = state;

            for(int endpointIndex = 0; endpointIndex < endPoints.Length; endpointIndex++)
            {
                var args = _socketEventArgsPool.Get();
                args.RemoteEndPoint = endPoints[endpointIndex];
                SetBufferEncrypted(args, buffer, length, groupId, keyInfo);
                args.UserToken = token;

                if(!_socket.SendToAsync(args))
                {
                    //completed syncronously
                    _socketEventArgsPool.Add(args); //release the memory back to the pool
                }
                else
                {
                    token.IncrementWaitingCount();
                }
            }
            lock(_asyncCompleteLock)
            {
                if(token.WaitingCount == 0) 
                {
                    token.Finish();
                    return;
                }
            }
        }

        public void SendRegisterResponse(Registration registration, IPEndPoint endPoint)
        {
            // Packet Type
            _sendBuffer[0] = (byte)RopuPacketType.RegistrationResponse;
            // User ID (uint32)
            _sendBuffer.WriteUint(registration.UserId, 1);
            // Codec (byte) (defined via an enum, this is the codec/bitrate used by the system, you must support it, 
            // this is required so the server doesn’t have to transcode, which is an expensive operation)
            _sendBuffer[7] = (byte)Codecs.Opus;
            // Bitrate (uint16)
            _sendBuffer.WriteUshort(8000, 8);

            SendToUserEncrypted(_sendBuffer.AsSpan(0,10), endPoint);
        }

        public void SendCallStartFailed(CallFailedReason reason, uint userId, IPEndPoint endPoint)
        {
            // Packet Type
            _sendBuffer[0] = (byte)RopuPacketType.CallStartFailed;
            // User ID (uint32) 
            _sendBuffer.WriteUint(userId, 1);
            //* Reason (byte) 0 = insufficient resources, 255 = other reason
            _sendBuffer[5] = (byte)CallFailedReason.InsufficientResources;

            SendToUserEncrypted(_sendBuffer.AsSpan(0,6), endPoint);
        }
    }
}