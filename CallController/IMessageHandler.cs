using System;
using System.Net;

namespace Ropu.CallController
{
    public interface IMessageHandler
    {
        void HandleStartGroupCall(ushort groupId, uint userId);
    }
}