using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ropu.Client
{
    class Program
    {
        const uint _userId = 1234;
        const ushort _rtpPort = 1000;
        const ushort _controlPort = 5061;
        const ushort _floorControlPort = 1002;

        static void Main(string[] args)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint controllingFunctionEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5060);

            var controllingFunctionClient = new ControllingFunctionClient(_controlPort, controllingFunctionEndpoint);
            controllingFunctionClient.StartListening();
            Console.WriteLine("Sending Registration");
            controllingFunctionClient.Register(_userId, _rtpPort, _controlPort, _floorControlPort);
            while(true)
            {
                System.Threading.Thread.Sleep(1000);
            }

        }

    }
}
