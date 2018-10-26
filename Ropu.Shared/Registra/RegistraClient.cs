using System;
using System.Threading.Tasks;
using Ropu.Shared.CallManagement;

namespace Ropu.Shared.Registra
{
    public class RegistraClient
    {
        readonly RegistraGroup[] _groups = new RegistraGroup[ushort.MaxValue];
        readonly ServiceDiscovery _serviceDiscovery;
        readonly CallManagementProtocol _callManagementProtocol;
        readonly FileClient _fileClient;

        public RegistraClient(ServiceDiscovery serviceDiscovery, CallManagementProtocol callManagementProtocol, FileClient fileClient)
        {
            _serviceDiscovery = serviceDiscovery;
            _callManagementProtocol = callManagementProtocol;
            _fileClient = fileClient;
        }

        public async Task SyncGroups()
        {
            Console.WriteLine("Syncing Groups");
            ushort numberOfParts = 0;
            ushort fileId = 0;

            bool gotResponse = false;
            while(!gotResponse)
            {
                var callManagementServerEndpoint = _serviceDiscovery.CallManagementServerEndpoint();
                
                Action<ushort, ushort> fileManifestHandler = (parts, id) =>
                {
                    numberOfParts = parts;
                    fileId = id;
                };
                gotResponse = await _callManagementProtocol.SendGetGroupsFileRequest(callManagementServerEndpoint, fileManifestHandler);

            }
            var groupIds = await _fileClient.RetrieveGroupsFile(fileId, numberOfParts, _serviceDiscovery.CallManagementServerEndpoint());
            for(int index = 0; index < groupIds.Count; index++)
            {
                var groupId = groupIds[index];
                _groups[groupId] = new RegistraGroup(groupId);
            }
            Console.WriteLine($"Retrieved {groupIds.Count} Groups");
        }

        RegistraGroup GetGroup(ushort groupId)
        {
            return _groups[groupId];
        }
    }
}