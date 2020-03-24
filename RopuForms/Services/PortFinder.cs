using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using System;

namespace Ropu.Shared
{
    public class MobilePortFinder : IPortFinder
    {
        int[] GetActivePortsInRange(int startingPort)
        {
            return new int[0]; //on android we can't know this so will just have to keep trying until it works.
        }

        public int BindToAvailablePort(Socket socket, IPAddress address, int startingPort)
        {
            var portsInRange = GetActivePortsInRange(startingPort);

            for (int candidatePort = startingPort; candidatePort < ushort.MaxValue; candidatePort++)
            {
                if (portsInRange.Contains(candidatePort))
                {
                    continue;
                }
                try
                {
                    socket.Bind(new IPEndPoint(address, candidatePort));
                    return candidatePort;
                }
                catch (SocketException)
                {
                    //race condition, someone else opened the port after we got the list
                    continue;
                }
            }
            throw new Exception("Ran out of ports");
        }
    }
}