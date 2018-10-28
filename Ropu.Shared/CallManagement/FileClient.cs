using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared.CallManagement;
using Ropu.Shared.Groups;
using Ropu.Shared.Registra;

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
                ReadOnlySpanAction<byte, FilePartFailureReason> handler = (payload, failureReason) => 
                {
                    GroupsHandler(payload, failureReason, groups);
                };

                while(!await _callManagementProtocol.SendGetFilePartRequest(fileId, partNumber, handler, targetEndPoint)){}
            }
            return groups;
        }

        void UsersHandler(ReadOnlySpan<byte> payload, FilePartFailureReason failureReason, List<uint> users)
        {
            for(int index = 0; index < payload.Length; index += 10)
            {
                var userId = payload.ParseUint();
                var endPoint = payload.ParseIPEndPoint();
                users.Add(userId);
                Console.WriteLine($"Added user {userId} IP {endPoint}");
            }
        }

        public async Task<List<uint>> RetrieveGroupFile(ushort fileId, ushort numberOfParts, IPEndPoint targetEndPoint)
        {
            var users = new List<uint>();
            for(ushort partNumber = 0; partNumber < numberOfParts; partNumber++)
            {
                ReadOnlySpanAction<byte, FilePartFailureReason> handler = (payload, failureReason) => 
                {
                    UsersHandler(payload, failureReason, users);
                };

                while(!await _callManagementProtocol.SendGetFilePartRequest(fileId, partNumber, handler, targetEndPoint)){}
            }
            return users;
        }
    }
}