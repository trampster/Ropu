using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared.CallManagement;
using Ropu.Shared.Groups;

namespace Ropu.Shared
{

    public class FileClient
    {
        readonly CallManagementProtocol _callManagementProtocol;

        public FileClient(CallManagementProtocol callManagementProtocol)
        {
            _callManagementProtocol = callManagementProtocol;
        }

        void GroupsHandler(ReadOnlySpan<byte> payload, FilePartFailureReason failureReason, List<ushort> groups)
        {
            for(int index =0; index < payload.Length; index+=2)
            {
                var groupId = payload.Slice(index).ParseUshort();
                groups.Add(groupId);
            }
        }

        public async Task<List<ushort>> RetrieveGroupsFile(ushort fileId, ushort numberOfParts, IPEndPoint targetEndPoint)
        {
            var groups = new List<ushort>();
            for(ushort partNumber = 0; partNumber < numberOfParts; partNumber++)
            {
                ReadOnlySpanAction<byte, FilePartFailureReason> handler = (payload, failureReason) => GroupsHandler(payload, failureReason, groups);

                while(!await _callManagementProtocol.SendGetFilePartRequest(fileId, partNumber, handler, targetEndPoint)){}
            }
            return groups;
        }
    }
}