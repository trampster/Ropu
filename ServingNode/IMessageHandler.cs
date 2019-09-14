using System;
using System.Net;
using System.Threading.Tasks;

namespace Ropu.ServingNode
{
    public interface IMessageHandler
    {
        Task Registration(uint userId, IPEndPoint endPoint);
        void Heartbeat(uint userId, IPEndPoint endPoint);
        void HandleCallControllerMessage(ushort groupId, byte[] packetData, int length);
        void ForwardPacketToClients(ushort groupId, byte[] packetData, int length, IPEndPoint from);
        void ForwardClientMediaPacket(ushort groupId, byte[] packetData, int length, IPEndPoint from);
        Task Deregister(uint userId, IPEndPoint endPoint);
        void HandleCallEnded(ushort groupId, byte[] buffer, int ammountRead, IPEndPoint endPoint);
        void ForwardFloorTaken(ushort groupId, byte[] buffer, int ammountRead, IPEndPoint endPoint);
        void ForwardFloorIdle(ushort groupId, byte[] buffer, int ammountRead, IPEndPoint endPoint);
    }
}