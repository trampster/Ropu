using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ropu.Client
{
    class Program
    {
        const ushort _controlPort = 5061;

        static void Main(string[] args)
        {
            IPEndPoint controllingFunctionEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5060);

            var controllingFunctionClient = new ControllingFunctionClient(_controlPort, controllingFunctionEndpoint);

            var ropuClient = new RopuClient(controllingFunctionClient);
            ropuClient.Start();

            while(true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

    }
}
