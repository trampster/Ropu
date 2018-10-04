using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ropu.Client
{
    class Program
    {
        const ushort _controlPort = 5061;
        static RopuClient _ropuClient;
        static void Main(string[] args)
        {
            IPEndPoint controllingFunctionEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5060);

            var controllingFunctionClient = new ControllingFunctionClient(_controlPort, controllingFunctionEndpoint);

            _ropuClient = new RopuClient(controllingFunctionClient);
            _ropuClient.Start();

            Console.Write(">");

            while(true)
            {
                var command = Console.ReadLine();
                HandleCommand(command);
                Console.Write(">");
            }
        }

        static void HandleCommand(string commandLine)
        {
            if(string.IsNullOrEmpty(commandLine))
            {
                return;
            }
            char command = commandLine[0];

            switch(command)
            {
                case 'g':
                    var group = uint.Parse(commandLine.AsSpan(1));
                    _ropuClient.StartCall(group);
                    break;
                default:
                    Console.WriteLine("I'm not sure what you mean.");
                    break;

            }
        }


    }
}
