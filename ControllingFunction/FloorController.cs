using System;
using System.Net;

namespace Ropu.ControllingFunction
{
    public class FloorController : IRegisteredController
    {

        IPEndPoint _floorEndPoint;
        DateTime _expirtyTime;

        readonly object _lock = new object();
        public FloorController(IPEndPoint controlEndPoint, IPEndPoint floorEndPoint)
        {
            ControlEndPoint = controlEndPoint;
            _floorEndPoint = floorEndPoint;
        }

        public IPEndPoint ControlEndPoint
        {
            get;
        }

        public IPEndPoint FloorEndPoint
        {
            get
            {
                return _floorEndPoint;
            }
        }

        void SetupExpiryTime()
        {
            _expirtyTime = DateTime.UtcNow.AddSeconds(120);
        }

        public void Update(IPEndPoint floorEndPoint)
        {
            lock(_lock)
            {
                _floorEndPoint = floorEndPoint;
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