using System;
using System.Net;
using System.Net.Sockets;

namespace Ropu.ContollingFunction
{
    public interface IControlPacket
    {
        ControlPacketType PacketType
        {
            get;
        }
    }
}