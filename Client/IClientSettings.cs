using System;
using System.Net;

namespace Ropu.Client
{
    public interface IClientSettings
    {
        uint? UserId{get;set;}

        bool FakeMedia
        {
            get;
            set;
        }

        string? FileMediaSource
        {
            get;
            set;
        }
    }
}