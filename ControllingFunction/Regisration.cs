using System;
using System.Net;
using System.Net.Sockets;

namespace Ropu.ContollingFunction
{
    public class Registration : IControlPacket
    {
        public Registration(uint userId, ushort rtpPort, IPEndPoint controlEndpoint)
        {
            UserId = userId;
            RtpPort = rtpPort;
            ControlEndpoint = controlEndpoint;
        }

        public ControlPacketType PacketType => ControlPacketType.Registration;


        public uint UserId
        {
            get;
        }

        public ushort RtpPort
        {
            get;
        }

        public IPEndPoint ControlEndpoint
        {
            get;
        }
    }
}