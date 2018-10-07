﻿using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared.CallManagement;
using Ropu.Shared.Groups;

namespace Ropu.ControllingFunction
{
    class Program
    {
        static void Main(string[] args)
        {
            var registra = new Registra();
            var controlProtocol = new ControlProtocol(5060);
            var callManagementProtocol = new CallManagementProtocol(5069);
            var groupsClient = new HardcodedGroupsClient();
            var controller = new Controller(controlProtocol, registra, callManagementProtocol, groupsClient);
            controller.Run();
        }   
    }
}
