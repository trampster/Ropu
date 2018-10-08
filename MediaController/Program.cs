using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ropu.MediaController;
using Ropu.Shared.CallManagement;

namespace ropu
{
    class Program
    {
        const ushort ControlPort = 5070;


        static async Task Main(string[] args)
        {
            var mediaProtocol = new MediaProtocol();
            var callManagementProtocol = new CallManagementProtocol(ControlPort);

            Task callManagementTask = callManagementProtocol.Run();
            Task mediaTask = mediaProtocol.Run();

            await Task.WhenAll(callManagementTask, mediaTask);
        }
    }
}
