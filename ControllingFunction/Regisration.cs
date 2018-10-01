using System;
using System.Net;
using System.Net.Sockets;

namespace Ropu.ContollingFunction
{
    public class Registration : IControlPacket
    {
        public Registration(uint userId, ushort rtpPort, ushort controlPlanePort)
        {
            UserId = userId;
            RtpPort = rtpPort;
            ControlPlanePort = controlPlanePort;
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

        public ushort ControlPlanePort
        {
            get;
        }
    }
}