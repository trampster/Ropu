using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.AsyncTools;
using Ropu.Shared.ControlProtocol;

namespace Ropu.CallController
{
    public class RopuProtocol
    {
        readonly Socket _socket;
        const int MaxUdpSize = 0x10000;
        const int AnyPort = IPEndPoint.MinPort;
        static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        readonly byte[] _receiveBuffer = new byte[MaxUdpSize];
        readonly PacketEncryption _packetEncryption;

        IMessageHandler? _messageHandler;

        readonly MemoryPool<byte[]> _sendBufferPool = new MemoryPool<byte[]>(() => new byte[1024]);

        public RopuProtocol(PortFinder portFinder, int startingPort, PacketEncryption packetEncryption)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            MediaPort = (ushort)portFinder.BindToAvailablePort(_socket, IPAddress.Any, startingPort);
            Console.WriteLine($"Serving Node Protocol bound to port {MediaPort}");

            _socketEventArgsPool = new MemoryPool<SocketAsyncEventArgs>(() => CreateSocketAsyncEventArgs());

            _packetEncryption = packetEncryption;
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
                try
                {
                    int payloadLength = await _packetEncryption.Decrypt(buffer, socketArgs.BytesTransferred, payload);

                    if(payloadLength != 0)
                    {
                        HandlePacket(payload, payloadLength, (IPEndPoint)socketArgs.RemoteEndPoint);
                    }
                }
                catch(Exception exception)
                {
                    Console.WriteLine($"Exception occured process ropu packet {exception.ToString()}");
                }
            }
        }

        void HandlePacket(byte[] buffer, int ammountRead, IPEndPoint endPoint)
        {
            var data = new Span<byte>(buffer, 0, ammountRead);

            switch((RopuPacketType)data[0])
            {
                case RopuPacketType.StartGroupCall:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    uint userId = data.Slice(3).ParseUint();
                    _messageHandler?.HandleStartGroupCall(groupId, userId);
                    break;
                }
                case RopuPacketType.FloorReleased:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    uint userId = data.Slice(3).ParseUint();
                    _messageHandler?.HandleFloorReleased(groupId, userId);
                    break;
                }
                case RopuPacketType.FloorRequest:
                {
                    ushort groupId = data.Slice(1).ParseUshort();
                    uint userId = data.Slice(3).ParseUint();
                    _messageHandler?.HandleFloorRequest(groupId, userId);
                    break;
                }
                default:
                {
                    Console.Error.WriteLine($"Received Unknown Ropu packet type {data[0]}");
                    break;
                }
            }
        }


        readonly MemoryPool<SocketAsyncEventArgs> _socketEventArgsPool;
        int _waitingSendCount = 0;
        Action? _onBulkAsyncFinished;
        readonly object _asyncCompleteLock = new object();

        SocketAsyncEventArgs CreateSocketAsyncEventArgs()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += AsyncSendComplete;
            return args;
        }

        void AsyncSendComplete(object? sender, SocketAsyncEventArgs args)
        {
            
            _socketEventArgsPool.Add(args);

            lock(_asyncCompleteLock)
            {
                int newCount = Interlocked.Decrement(ref _waitingSendCount);
                if(newCount == 0)
                {
                    _onBulkAsyncFinished?.Invoke();
                    _onBulkAsyncFinished = null;
                }
            }
        }

        internal void SendFloorDenied(ushort groupId, uint userId)
        {
            throw new NotImplementedException();
        }

        void BulkSendAsync(byte[] buffer, int length, Span<IPEndPoint> endPoints, Action onComplete)
        {
            for(int endpointIndex = 0; endpointIndex < endPoints.Length; endpointIndex++)
            {
                var args = _socketEventArgsPool.Get();
                args.RemoteEndPoint = endPoints[endpointIndex];
                args.SetBuffer(buffer, 0, length);

                if(!_socket.SendToAsync(args))
                {
                    Interlocked.Increment(ref _waitingSendCount);
                    //completed syncronously
                    _socketEventArgsPool.Add(args); //release the memory back to the pool
                }
            }
            lock(_asyncCompleteLock)
            {
                if(_waitingSendCount == 0) 
                {
                    onComplete();
                    return;
                }
                _onBulkAsyncFinished = onComplete;
            }
        }

        public void SendCallEnded(ushort groupId, Span<IPEndPoint> endPoints, CachedEncryptionKey keyInfo)
        {
            var buffer = _sendBufferPool.Get();
            // Packet Type
            buffer[0] = (byte)RopuPacketType.CallEnded;
            // Group ID (uint16)
            buffer.WriteUshort(groupId, 1);

            BulkSendEncrypted(buffer.AsSpan(0, 3), groupId, endPoints, keyInfo);

            _sendBufferPool.Add(buffer);
        }

        public void SendCallStartFailed(CallFailedReason reason, uint userId, IPEndPoint endPoint, CachedEncryptionKey keyInfo)
        {
            var buffer = _sendBufferPool.Get();
            // Packet Type
            buffer[0] = (byte)RopuPacketType.CallStartFailed;
            // User ID (uint32) 
            buffer.WriteUint(userId, 1);
            //* Reason (byte) 0 = insufficient resources, 255 = other reason
            buffer[5] = (byte)CallFailedReason.InsufficientResources;

            SendToEncrypted(buffer.AsSpan(0, 6), userId, endPoint, keyInfo);
            _sendBufferPool.Add(buffer);
        }

        void SendToEncrypted(Span<byte> payload, uint userId, IPEndPoint endPoint, CachedEncryptionKey keyInfo)
        {
            var packet = _sendBufferPool.Get();
            int packetLength = _packetEncryption.CreateEncryptedPacket(payload, packet, false, userId, keyInfo);

            _socket.SendTo(packet, 0, packetLength, SocketFlags.None, endPoint);
            _sendBufferPool.Add(packet);
        }

        public void SendFloorTaken(uint userId, ushort groupId, Span<IPEndPoint> endPoints, CachedEncryptionKey keyInfo)
        {
            var buffer = _sendBufferPool.Get();
            // Packet Type
            buffer[0] = (byte)RopuPacketType.FloorTaken;
            // Group ID (ushort) 
            buffer.WriteUshort(groupId, 1);
            // User ID (uint32) 
            buffer.WriteUint(userId, 3);

            BulkSendEncrypted(buffer.AsSpan(0, 7), groupId, endPoints, keyInfo);
            _sendBufferPool.Add(buffer);
        }

        public void SendFloorIdle(ushort groupId, Span<IPEndPoint> endPoints, CachedEncryptionKey keyInfo)
        {
            var buffer = _sendBufferPool.Get();
            // Packet Type
            buffer[0] = (byte)RopuPacketType.FloorIdle;
            // Group ID (ushort) 
            buffer.WriteUshort(groupId, 1);

            BulkSendEncrypted(buffer.AsSpan(0, 3), groupId, endPoints, keyInfo);
            _sendBufferPool.Add(buffer);
        }

        void BulkSendEncrypted(Span<byte> payload, ushort groupId, Span<IPEndPoint> endPoints, CachedEncryptionKey keyInfo)
        {
            var packet = _sendBufferPool.Get();
            int packetLength = _packetEncryption.CreateEncryptedPacket(payload, packet, true, groupId, keyInfo);

            BulkSendAsync(packet, packetLength, endPoints, () => _sendBufferPool.Add(packet));
        }
    }
}