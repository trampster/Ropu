using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ropu.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(new IPEndPoint(IPAddress.Any, 5061));


            const int MaxUDPSize = 0x10000;

            byte[] buffer = new byte[MaxUDPSize];

            EndPoint mediaControllerEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5060);

            var payload = new byte[]
            {
                1,2,3,4,5,6,7,8,9,10
            };
            while(true)
            {
                int length = BuildMediaPacket(1234, payload, buffer);
                socket.SendTo(buffer, 0, length, SocketFlags.None, mediaControllerEndpoint);
                System.Threading.Thread.Sleep(5000);
            }
        }

        static int BuildMediaPacket(ushort callId, byte[] payload, byte[] buffer)
        {
            buffer[0] = (byte)((callId & 0xFF00) >> 8);
            buffer[1] = (byte)(callId & 0xFF);

            int bufferIndex = 2;
            for(int payloadIndex = 0; payloadIndex < payload.Length; payloadIndex++)
            {
                buffer[bufferIndex] = payload[payloadIndex];
                bufferIndex++;
            }
            return bufferIndex;
        }
    }
}
