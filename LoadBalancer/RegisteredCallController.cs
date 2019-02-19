using System;
using System.Collections.Generic;
using System.Net;

namespace Ropu.LoadBalancer
{
    public class RegisteredCallController : IRegisteredController
    {
        IPEndPoint _callEndPoint;
        DateTime _expirtyTime;
        readonly object _lock = new object();

        public RegisteredCallController(IPEndPoint loadBalancerEndPoint, IPEndPoint floorEndPoint)
        {
            LoadBalancerEndPoint = loadBalancerEndPoint;
            _callEndPoint = floorEndPoint;
            SetupExpiryTime();
        }

        public IPEndPoint LoadBalancerEndPoint
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
            _expirtyTime = DateTime.UtcNow.AddSeconds(15); //10 seconds + 3 for three retires plus 2 for margin.
        }

        public void RefreshExpiry()
        {
            lock(_lock)
            {
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

        int _maxCapacity = 100;

        readonly List<ushort> _groups = new List<ushort>();

        public void AddGroup(ushort group)
        {
            _groups.Add(group);
        }

        public IEnumerable<ushort> Groups
        {
            get
            {
                return _groups;
            }
        }

        public bool HasCapacity()
        {
            return _groups.Count < _maxCapacity;
        }
    }
}