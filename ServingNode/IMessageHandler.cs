using System;
using System.Net;

namespace Ropu.ServingNode
{
    public interface IMessageHandler
    {
        void Registration(uint userId, IPEndPoint endPoint);
        void Heartbeat(uint userId, IPEndPoint endPoint);
        void HandleCallControllerMessage(ushort groupId, byte[] packetData, int length);
        void HandleMediaPacket(ushort groupId, byte[] packetData, int length, IPEndPoint from);
        void Deregister(uint userId, IPEndPoint endPoint);
    }
}