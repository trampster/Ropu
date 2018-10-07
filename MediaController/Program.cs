using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ropu
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(new IPEndPoint(IPAddress.Any, 5062));


            const int MaxUDPSize = 0x10000;
            const int AnyPort = IPEndPoint.MinPort;

            byte[] buffer = new byte[MaxUDPSize];

            EndPoint endpoint = new IPEndPoint(IPAddress.Any, AnyPort);

            _endpoints = new IPEndPoint[10000];

            for(int endpointIndex = 0; endpointIndex < _endpoints.Length; endpointIndex++)
            {
                _endpoints[endpointIndex] = new IPEndPoint(IPAddress.Parse("192.168.1.2"), endpointIndex + 1000);
            }

            while(true)
            {
                Console.WriteLine("waiting for packet");
                int length = socket.ReceiveFrom(buffer, MaxUDPSize, 0, ref endpoint);
                HandlePacket(buffer, length, socket);
            }

        }

        static ushort ParseUshort(Span<byte> span)
        {
            return (ushort)(
                (span[0] << 8) +
                span[1]);
        }

        static IPEndPoint[] _endpoints;

        static void HandlePacket(byte[] buffer, int length, Socket socket)
        {
            //find the call id
            ushort callId = ParseUshort(new Span<byte>(buffer, 0, 2));
            Console.WriteLine($"Received Packet with call id {callId}");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            BulkSendSync(buffer, length, socket);

            stopwatch.Stop();
            Console.WriteLine($"Forwarded {_endpoints.Length} packets in {stopwatch.ElapsedMilliseconds} ms");

            //send to all the group endpoints
        }

        static void BulkSendAsync(byte[] buffer, int length, Socket socket)
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

        static void BulkSendParallelFor(byte[] buffer, int length, Socket socket)
        {
            Parallel.For(0, _endpoints.Length, new ParallelOptions(){MaxDegreeOfParallelism = 40}, endpointIndex => 
                socket.SendTo(buffer, 0, length, SocketFlags.None, _endpoints[endpointIndex]));
        }

        static void BulkSendSync(byte[] buffer, int length, Socket socket)
        {
            for(int endpointIndex = 0; endpointIndex < _endpoints.Length; endpointIndex++)
            {
                socket.SendTo(buffer, 0, length, SocketFlags.None, _endpoints[endpointIndex]);
            }
        }
    }
}
