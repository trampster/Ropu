using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared.ControlProtocol;

namespace Ropu.LoadBalancer
{
    public class Registration
    {
        public Registration(uint userId)
        {
            UserId = userId;
        }

        public uint UserId
        {
            get;
        }
    }
}