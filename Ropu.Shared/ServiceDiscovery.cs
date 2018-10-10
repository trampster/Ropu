using System;
using System.Net;
using System.Net.Sockets;

namespace Ropu.Shared
{
    public class ServiceDiscovery
    {
        public IPEndPoint CallManagementServerEndpoint()
        {
            return new IPEndPoint(IPAddress.Parse("172.16.182.32"), 5069);
        }

        public IPAddress GetMyAddress()
        {
            return IPAddress.Parse("172.16.182.32");
        }
    }
}