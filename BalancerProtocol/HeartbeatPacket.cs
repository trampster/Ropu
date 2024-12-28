using System.Net;

namespace Ropu.BalancerProtocol;

public struct HeartbeatPacket
{
    public ushort Id
    {
        get;
        set;
    }

    public ushort NumberRegistered
    {
        get;
        set;
    }
}
