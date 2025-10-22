using System.Net;

namespace Ropu.Protocol;

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
