using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        IMessageHandler _messageHandler;

        MemoryPool<byte[]> _sendBufferPool = new MemoryPool<byte[]>(() => new byte[ushort.MaxValue]);

        public RopuProtocol(PortFinder portFinder, int startingPort)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            MediaPort = (ushort)portFinder.BindToAvailablePort(_socket, IPAddress.Any, startingPort);
            Console.WriteLine($"Serving Node Protocol bound to port {MediaPort}");

            _socketEventArgsPool = new MemoryPool<SocketAsyncEventArgs>(() => CreateSocketAsyncEventArgs());
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

                HandlePacket(buffer, socketArgs.BytesTransferred, (IPEndPoint)socketArgs.RemoteEndPoint);
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
            }
        }

        public void SendPacket(byte[] packet, int length, IPEndPoint target)
        {
            _socket.SendTo(packet, 0, length, SocketFlags.None, target);
        }

        readonly MemoryPool<SocketAsyncEventArgs> _socketEventArgsPool;
        int _waitingSendCount = 0;
        Action _onBulkAsyncFinished;
        readonly object _asyncCompleteLock = new object();

        SocketAsyncEventArgs CreateSocketAsyncEventArgs()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += AsyncSendComplete;
            return args;
        }

        void AsyncSendComplete(object sender, SocketAsyncEventArgs args)
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

        public void BulkSendAsync(byte[] buffer, int length, Span<IPEndPoint> endPoints, Action onComplete, IPEndPoint except)
        {
            for(int endpointIndex = 0; endpointIndex < endPoints.Length; endpointIndex++)
            {
                var args = _socketEventArgsPool.Get();
                var endPoint = endPoints[endpointIndex];
                if(endPoint == except)
                {
                    continue;
                }
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

        public void BulkSendAsync(byte[] buffer, int length, Span<IPEndPoint> endPoints, Action onComplete)
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

        public void SendCallStarted(
            uint userId, ushort groupId,
            Span<IPEndPoint> endPoints)
        {
            var buffer = _sendBufferPool.Get();
            // Packet Type
            buffer[0] = (byte)RopuPacketType.GroupCallStarted;
            // Group ID (uint16)
            buffer.WriteUshort(groupId, 1);
            // User Id (uint32)
            buffer.WriteUint(userId, 3);

            BulkSendAsync(buffer, 7, endPoints, () => _sendBufferPool.Add(buffer));
        }

        public void SendCallStartFailed(CallFailedReason reason, uint userId, IPEndPoint endPoint)
        {
            var buffer = _sendBufferPool.Get();
            // Packet Type
            buffer[0] = (byte)RopuPacketType.CallStartFailed;
            // User ID (uint32) 
            buffer.WriteUint(userId, 1);
            //* Reason (byte) 0 = insufficient resources, 255 = other reason
            buffer[5] = (byte)CallFailedReason.InsufficientResources;

            _socket.SendTo(buffer, 0, 6, SocketFlags.None, endPoint);

            _sendBufferPool.Add(buffer);
        }
    }
}