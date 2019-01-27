using System;
using Eto.Forms;
using Ropu.Client;

namespace Ropu.ClientUI
{
    public class RopuApplication : Application
    {
        readonly RopuClient _ropuClient;
        public RopuApplication(RopuClient ropuClient)
        {
            _ropuClient = ropuClient;
        }

        protected override async void OnInitialized(EventArgs e)
        {
            await _ropuClient.Run();
        }
    }
}