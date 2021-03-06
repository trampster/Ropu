using System;
using System.Net;
using System.Net.Sockets;
using Ropu.Shared.ControlProtocol;

namespace Ropu.ServingNode
{
    public class Registration
    {
        DateTime _lastSeen;

        public Registration(uint userId, IPEndPoint endPoint)
        {
            UserId = userId;
            EndPoint = new UserIPEndPoint(UserId, endPoint);
        }

        public uint UserId
        {
            get;
        }

        public IPEndPoint EndPoint
        {
            get;
        }

        public DateTime LastSeen => _lastSeen;

        public void Renew()
        {
            _lastSeen = DateTime.UtcNow;
        }
    }
}