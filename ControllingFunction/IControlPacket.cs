using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared.ControlProtocol;

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