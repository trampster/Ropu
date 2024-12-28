using System.Net;
using System.Net.Sockets;

namespace Ropu.Balancer;


public class Server
{
    bool _isUsed = false;
    public bool IsUsed
    {
        get => _isUsed;
        set
        {
            if (value)
            {
                Seen = true;
                NumberRegistered = 0;
            }
            else
            {
                Seen = false;
            }
            _isUsed = value;
        }
    }

    public ushort Id
    {
        get;
        set;
    }

    public SocketAddress Endpoint
    {
        get;
        set;
    } = new SocketAddress(AddressFamily.InterNetwork);

    public ushort Capacity
    {
        get;
        set;
    }

    public ushort NumberRegistered
    {
        get;
        set;
    }

    public bool Seen
    {
        get;
        set;
    } = true;
}