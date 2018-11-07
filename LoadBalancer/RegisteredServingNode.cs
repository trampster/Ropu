using System;
using System.Net;

namespace Ropu.LoadBalancer
{
    public class RegisteredServingNode : IRegisteredController
    {
        IPEndPoint _servingEndPoint;
        DateTime _expirtyTime;

        readonly object _lock = new object();

        public RegisteredServingNode(IPEndPoint controlEndPoint, IPEndPoint mediaEndPoint)
        {
            ControlEndPoint = controlEndPoint;
            _servingEndPoint = mediaEndPoint;
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

        public IPEndPoint ServingEndPoint
        {
            get
            {
                return _servingEndPoint;
            }
        }

        public void Update(IPEndPoint mediaEndPoint)
        {
            lock(_lock)
            {
                _servingEndPoint = mediaEndPoint;
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