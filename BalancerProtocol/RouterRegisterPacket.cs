using System.Net;

namespace Ropu.BalancerProtocol;

public struct RouterRegisterPacket
{
    public IPEndPoint EndPoint
    {
        get;
        set;
    }

    public ushort Capacity
    {
        get;
        set;
    }
}
