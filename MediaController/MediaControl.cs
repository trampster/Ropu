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

        public MediaControl(MediaProtocol mediaProtocol, CallManagementProtocol callManagementProtocol, ServiceDiscovery serviceDiscovery)
        {
            _mediaProtocol = mediaProtocol;
            _callManagementProtocol = callManagementProtocol;
            _serviceDiscovery = serviceDiscovery;
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
            bool gotResponse = false;
            while(!gotResponse)
            {
                var callManagementServerEndpoint = _serviceDiscovery.CallManagementServerEndpoint();
                ushort numberOfParts;
                ushort fileId;
                Action<ushort, ushort> fileManifestHandler = (parts, id) =>
                {
                    numberOfParts = parts;
                    fileId = id;
                }
                gotResponse = await _callManagementProtocol.SendGetGroupsFileRequest(callManagementServerEndpoint, fileManifestHandler);

            }
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