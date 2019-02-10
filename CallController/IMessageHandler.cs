using System;
using System.Net;

namespace Ropu.CallController
{
    public interface IMessageHandler
    {
        void HandleStartGroupCall(ushort groupId, uint userId);
        void HandleFloorReleased(ushort groupId, uint userId);
        void HandleFloorRequest(ushort groupId, uint userId);
    }
}