using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared.ControlProtocol;

namespace Ropu.ControllingFunction
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