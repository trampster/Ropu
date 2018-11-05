using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.CallManagement;

namespace Ropu.Client
{
    class Program
    {
        const ushort _controlPort = 5061;
        static RopuClient _ropuClient;
        const string ServerIP =  "192.168.1.6";
        const string MyAddress = "192.168.1.6";
        const int ServerPort = 5060;
        static MediaClient _mediaClient;
        static async Task Main(string[] args)
        {

            var protocolSwitch = new ProtocolSwitch(_controlPort);
            var controllingFunctionClient = new ControllingFunctionClient(protocolSwitch);
            _mediaClient = new MediaClient(protocolSwitch);
            var callManagementProtocol = new CallManagementProtocol(5079);

            var ipAddress = IPAddress.Parse(MyAddress);

            IPEndPoint loadBallancerEndpoint = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);
            _ropuClient = new RopuClient(protocolSwitch, controllingFunctionClient, ipAddress, callManagementProtocol, loadBallancerEndpoint);
            var ropuClientTask = _ropuClient.Run();

            var consoleTask = TaskCordinator.RunLong(HandleCommands);
            
            await TaskCordinator.WaitAll(ropuClientTask, consoleTask);
        }

        static void HandleCommands()
        {
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
                    var group = ushort.Parse(commandLine.AsSpan(1));
                    _ropuClient.StartCall(group);
                    break;
                case 'm':
                    SendMedia();
                    break;
                default:
                    Console.WriteLine("I'm not sure what you mean.");
                    break;

            }
        }

        static void SendMedia()
        {
            var ipAddress = IPAddress.Parse(ServerIP);
            var endPoint = new IPEndPoint(ipAddress, 5065);
            var payload = new byte[] {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20};
            while(true)
            {
                _mediaClient.SendMediaPacket(13, payload, endPoint);
                System.Threading.Thread.Sleep(200);
            }
        }


    }
}
