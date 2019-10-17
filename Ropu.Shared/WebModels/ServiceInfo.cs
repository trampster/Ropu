using System;

namespace Ropu.Shared.WebModels
{
    public enum ServiceType
    {
        LoadBalancer,
        ServingNode,
        CallController
    }

    public class ServiceInfo
    {
        public ServiceType ServiceType
        {
            get;
            set;
        }

        public uint UserId
        {
            get;
            set;
        }

        public byte? ServiceId
        {
            get;
            set;
        }
    }
}