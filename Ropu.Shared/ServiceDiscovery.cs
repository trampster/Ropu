using System;
using System.Net;
using System.Net.Sockets;

namespace Ropu.Shared
{
    public class ServiceDiscovery
    {
        public IPEndPoint CallManagementServerEndpoint()
        {
            return new IPEndPoint(GetMyAddress(), 5069);
        }

        public IPAddress GetMyAddress()
        {
            foreach (var address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address;
                }
            }
            throw new Exception("Failed to find my IP Address");
        }
    }

}