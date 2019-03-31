using System;
using System.Net;

namespace Ropu.Client
{
    public interface IClientSettings
    {
        uint UserId{get;set;}

        event EventHandler UserIdChanged;

        bool FakeMedia
        {
            get;
            set;
        }

        string FileMediaSource
        {
            get;
            set;
        }

        IPAddress LoadBalancerIPAddress
        {
            get;
            set;
        }

    }
}