using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Shared;

namespace Ropu.ServingNode
{
    public class MediaProtocol
    {
        IPEndPoint[] _endpoints;
        readonly Socket _socket;
        const int MaxUDPSize = 0x10000;
        const int AnyPort = IPEndPoint.MinPort;

        readonly byte[] _receiveBuffer = new byte[MaxUDPSize];


        public MediaProtocol(ushort port)
        {
            MediaPort = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, MediaPort));

            //setup dummy group endpoints
            _endpoints = new IPEndPoint[10000];
            for(int endpointIndex = 0; endpointIndex < _endpoints.Length; endpointIndex++)
            {
                _endpoints[endpointIndex] = new IPEndPoint(IPAddress.Parse("192.168.1.2"), endpointIndex + 1000);
            }

        }

        public ushort MediaPort
        {
            get;
        }

        public async Task Run()
        {
            var task = new Task(HandlePackets, TaskCreationOptions.LongRunning);
            task.Start();
            await task;
        }

        void HandlePackets()
        {
            EndPoint endpoint = new IPEndPoint(IPAddress.Any, AnyPort);

            while(true)
            {
                Console.WriteLine("waiting for packet");
                int length = _socket.ReceiveFrom(_receiveBuffer, MaxUDPSize, 0, ref endpoint);
                HandlePacket(_receiveBuffer, length, _socket);
            }
        }

        void HandlePacket(byte[] buffer, int length, Socket socket)
        {
            var span = buffer.AsSpan();

            //find the call id
            ushort callId = span.Slice(0).ParseUshort();
            Console.WriteLine($"Received Packet with call id {callId}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            BulkSendSync(buffer, length, socket);

            stopwatch.Stop();
            Console.WriteLine($"Forwarded {_endpoints.Length} packets in {stopwatch.ElapsedMilliseconds} ms");
        }

        void BulkSendAsync(byte[] buffer, int length, Socket socket)
        {
            for(int endpointIndex = 0; endpointIndex < _endpoints.Length; endpointIndex++)
            {
                var args = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = _endpoints[endpointIndex],
                };
                args.SetBuffer(buffer, 0, length);

                socket.SendAsync(args);
            }
        }

        void BulkSendParallelFor(byte[] buffer, int length, Socket socket)
        {
            Parallel.For(0, _endpoints.Length, new ParallelOptions(){MaxDegreeOfParallelism = 40}, endpointIndex => 
                socket.SendTo(buffer, 0, length, SocketFlags.None, _endpoints[endpointIndex]));
        }

        void BulkSendSync(byte[] buffer, int length, Socket socket)
        {
            for(int endpointIndex = 0; endpointIndex < _endpoints.Length; endpointIndex++)
            {
                socket.SendTo(buffer, 0, length, SocketFlags.None, _endpoints[endpointIndex]);
            }
        }
    }
}