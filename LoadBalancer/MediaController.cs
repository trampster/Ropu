using System;
using System.Net;

namespace Ropu.LoadBalancer
{
    public class MediaController : IRegisteredController
    {
        IPEndPoint _mediaEndpoint;
        DateTime _expirtyTime;

        readonly object _lock = new object();

        public MediaController(IPEndPoint controlEndPoint, IPEndPoint mediaEndPoint)
        {
            ControlEndPoint = controlEndPoint;
            _mediaEndpoint = mediaEndPoint;
            SetupExpiryTime();
        }

        void SetupExpiryTime()
        {
            _expirtyTime = DateTime.UtcNow.AddSeconds(120);
        }

        public IPEndPoint ControlEndPoint
        {
            get;
        }

        public IPEndPoint MediaEndPoint
        {
            get
            {
                return _mediaEndpoint;
            }
        }

        public void Update(IPEndPoint mediaEndPoint)
        {
            lock(_lock)
            {
                _mediaEndpoint = mediaEndPoint;
                SetupExpiryTime();
            }
        }

        public bool IsExpired()
        {
            lock(_lock)
            {
                return _expirtyTime < DateTime.UtcNow;
            }
        }
    }
}