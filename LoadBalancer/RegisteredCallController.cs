using System;
using System.Net;

namespace Ropu.LoadBalancer
{
    public class RegisteredCallController : IRegisteredController
    {

        IPEndPoint _callEndPoint;
        DateTime _expirtyTime;

        readonly object _lock = new object();
        public RegisteredCallController(IPEndPoint controlEndPoint, IPEndPoint floorEndPoint)
        {
            ControlEndPoint = controlEndPoint;
            _callEndPoint = floorEndPoint;
            SetupExpiryTime();
        }

        public IPEndPoint ControlEndPoint
        {
            get;
        }

        public IPEndPoint CallEndPoint
        {
            get
            {
                return _callEndPoint;
            }
        }

        void SetupExpiryTime()
        {
            _expirtyTime = DateTime.UtcNow.AddSeconds(120);
        }

        public void Update(IPEndPoint callEndPoint)
        {
            lock(_lock)
            {
                _callEndPoint = callEndPoint;
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