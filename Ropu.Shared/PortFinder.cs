using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using System;

namespace Ropu.Shared
{
    public class PortFinder
    {
        public int BindToAvailablePort(Socket socket, IPAddress address, int startingPort)
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            var portsInRange = properties.GetActiveUdpListeners()
                .Where(endPoint => endPoint.Port >= startingPort)
                .Select(endPoint => endPoint.Port)
                .ToArray();

            for(int candidatePort = startingPort; candidatePort < ushort.MaxValue; candidatePort++)
            {
                if(portsInRange.Contains(candidatePort))
                {
                    continue;
                }
                try
                {
                    socket.Bind(new IPEndPoint(address, candidatePort));
                    return candidatePort;
                }
                catch(SocketException)
                {
                    //range condition, someone else opened the port after we got the list
                    continue;
                }
            }
            throw new Exception("Ran out of ports");
        }
    }
}