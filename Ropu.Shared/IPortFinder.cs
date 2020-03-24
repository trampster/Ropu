using System.Net;
using System.Net.Sockets;

namespace Ropu.Shared
{
    public interface IPortFinder
    {
        int BindToAvailablePort(Socket socket, IPAddress address, int startingPort);
    }
}