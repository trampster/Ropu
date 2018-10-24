using System;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.CallManagement;

namespace Ropu.MediaController
{
    public class MediaControl
    {
        readonly MediaProtocol _mediaProtocol;
        readonly CallManagementProtocol _callManagementProtocol;
        readonly ServiceDiscovery _serviceDiscovery;
        readonly FileClient _fileClient;

        public MediaControl(
            MediaProtocol mediaProtocol, 
            CallManagementProtocol callManagementProtocol, 
            ServiceDiscovery serviceDiscovery,
            FileClient fileClient)
        {
            _mediaProtocol = mediaProtocol;
            _callManagementProtocol = callManagementProtocol;
            _serviceDiscovery = serviceDiscovery;
            _fileClient = fileClient;
        }

        public async Task Run()
        {
            Task callManagementTask = _callManagementProtocol.Run();
            Task mediaTask = _mediaProtocol.Run();

            //sync groups
            await SyncGroups();

            Task registerTask = Register();


            await TaskCordinator.WaitAll(callManagementTask, mediaTask, registerTask);
        }

        async Task SyncGroups()
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
            Console.WriteLine("RetrieveGroupsFile");
            var groups = await _fileClient.RetrieveGroupsFile(fileId, numberOfParts, _serviceDiscovery.CallManagementServerEndpoint());
        }

        async Task Register()
        {
            while(true)
            {
                var callManagementServerEndpoint = _serviceDiscovery.CallManagementServerEndpoint();
                bool registered = await _callManagementProtocol.RegisterMediaController(
                    _callManagementProtocol.ControlPort, 
                    new IPEndPoint(_serviceDiscovery.GetMyAddress(), _mediaProtocol.MediaPort), 
                    callManagementServerEndpoint);
                if(registered)
                {
                    Console.WriteLine("Registered");
                    await Task.Delay(60000);
                    continue;
                }
                Console.WriteLine("Failed to register");
            }
        }
           
    }
}