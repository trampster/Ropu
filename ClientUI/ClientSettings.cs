using System;
using Ropu.Client;

namespace Ropu.ClientUI
{
    public class ClientSettings : IClientSettings
    {
        uint _userId;
        public uint UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                UserIdChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler UserIdChanged;
    }
}