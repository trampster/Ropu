using System.Net;

namespace Ropu.Balancer;

public class Router
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

    public IPEndPoint Endpoint
    {
        get;
        set;
    } = new IPEndPoint(IPAddress.None, 0);

    public int Capacity
    {
        get;
        set;
    }

    public int NumberRegistered
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