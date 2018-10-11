using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared.ControlProtocol;

namespace Ropu.ControllingFunction
{
    public class Registration
    {
        public Registration(uint userId, IPEndPoint endPoint)
        {
            UserId = userId;
            EndPoint = endPoint;
        }

        public uint UserId
        {
            get;
        }

        public IPEndPoint EndPoint
        {
            get;
        }
    }
}