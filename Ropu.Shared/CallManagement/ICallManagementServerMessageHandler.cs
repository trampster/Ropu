using System;
using System.Net;

namespace Ropu.Shared.CallManagement
{
    public interface ICallManagementClientMessageHandler
    {
        void HandleCallStart(uint requestId, ushort callId, ushort groupId);
        void HandleFileManifestResponse(uint requestId, ushort numberOfParts, ushort fileId);
        void HandleFilePartResponse(uint requestId, Span<byte> payload);
        void HandleRegistrationUpdate(uint requestId, ushort groupId, uint userId, IPEndPoint endPoint);
        void HandleRegistrationRemoved(uint requestId, ushort groupId, uint userId);
        void HandleFilePartUnrecognized(uint requestId, FilePartFailureReason reason);
    }
}