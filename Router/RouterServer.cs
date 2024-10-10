
using System.Net;
using System.Net.Sockets;

namespace Ropu.Router;

public class RouterServer
{
    readonly byte[] _buffer = new byte[1024];

    public RouterServer()
    {

    }

    public async Task Run()
    {
        var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, 8001);
        socket.Bind(endpoint);

        while (true)
        {
            var recieved = await socket.ReceiveAsync(_buffer); //TODO: Should be ReceiveFrom
            if (recieved == 0)
            {
                continue;
            }

        }
    }
}