using System;

namespace Ropu.Client
{
    public interface IClientSettings
    {
        uint UserId{get;set;}

        event EventHandler UserIdChanged;
    }
}