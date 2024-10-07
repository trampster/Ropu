using System.Net;

namespace Ropu.BalancerProtocol;

public struct HeartbeatPacket
{
    public ushort RouterId
    {
        get;
        set;
    }

    public ushort RegisteredUsers
    {
        get;
        set;
    }
}
