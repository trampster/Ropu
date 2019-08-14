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
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address;
            }
        }
    }

}