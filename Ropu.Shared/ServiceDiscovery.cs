using System;
using System.Net;
using System.Net.Sockets;

namespace Ropu.Shared
{
    public class ServiceDiscovery
    {
        public IPEndPoint CallManagementServerEndpoint()
        {
            return new IPEndPoint(IPAddress.Parse("192.168.1.6"), 5069);
        }

        public IPAddress GetMyAddress()
        {
            return IPAddress.Parse("192.168.1.6");
        }
    }
}