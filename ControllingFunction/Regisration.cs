using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared.ControlProtocol;

namespace Ropu.ContollingFunction
{
    public class Registration
    {
        public Registration(uint userId, ushort rtpPort, ushort floorControlPort, IPEndPoint controlEndpoint)
        {
            UserId = userId;
            RtpPort = rtpPort;
            ControlEndpoint = controlEndpoint;
            FloorControlPort = floorControlPort;
        }

        public uint UserId
        {
            get;
        }

        public ushort RtpPort
        {
            get;
        }

        public ushort FloorControlPort
        {
            get;
        }

        public IPEndPoint ControlEndpoint
        {
            get;
        }
    }
}