using System;
using System.Net;
using Ropu.Client;

namespace Ropu.ClientUI
{
    public class ClientSettings : IClientSettings
    {
        uint? _userId;
        public uint? UserId
        {
            get => _userId;
            set
            {
                _userId = value;
            }
        }

        public bool FakeMedia { get; set; }
        public string FileMediaSource { get; set; }
    }
}