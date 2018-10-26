using System;
using System.Net;
using System.Threading.Tasks;
using Ropu.Shared;
using Ropu.Shared.CallManagement;
using Ropu.Shared.Registra;

namespace Ropu.MediaController
{
    public class MediaControl
    {
        readonly MediaProtocol _mediaProtocol;
        readonly CallManagementProtocol _callManagementProtocol;
        readonly ServiceDiscovery _serviceDiscovery;
        readonly RegistraClient _registraClient;

        public MediaControl(
            MediaProtocol mediaProtocol, 
            CallManagementProtocol callManagementProtocol, 
            ServiceDiscovery serviceDiscovery,
            RegistraClient registraClient)
        {
            _mediaProtocol = mediaProtocol;
            _callManagementProtocol = callManagementProtocol;
            _serviceDiscovery = serviceDiscovery;
            _registraClient = registraClient;
        }

        public async Task Run()
        {
            Task callManagementTask = _callManagementProtocol.Run();
            Task mediaTask = _mediaProtocol.Run();

            //sync groups
            await _registraClient.SyncGroups();

            Task registerTask = Register();


            await TaskCordinator.WaitAll(callManagementTask, mediaTask, registerTask);
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