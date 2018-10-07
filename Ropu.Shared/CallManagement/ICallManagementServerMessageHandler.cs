using System.Net;

namespace Ropu.Shared.CallManagement
{
    public interface ICallManagementClientMessageHandler
    {
        void CallStart(uint requestId, ushort callId, ushort groupId);
    }
}