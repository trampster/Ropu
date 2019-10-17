using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Ropu.Shared.WebModels;

namespace Ropu.Web.Services
{

    public class Service : ServiceInfo
    {
        public DateTime ExpiryTime
        {
            get;
            set;
        }

        public bool Expired
        {
            get
            {
                return ExpiryTime <= DateTime.UtcNow;
            }
        }
    }

    public class ServicesService
    {
        readonly Dictionary<byte, Service> _services = new Dictionary<byte, Service>();
        readonly ILogger _logger;


        public ServicesService(ILogger<ServicesService> logger)
        {
            _logger = logger;
            _logger.LogWarning("ServicesService constructor called");
        }

        public LoadBalancerInfo? LoadBalancerInfo
        {
            get;
            set;
        }

        public byte RegisterService(ServiceInfo serviceInfo)
        {
            lock(_lock)
            {
                List<byte> toRemove = new List<byte>();
                foreach(var serviceId in _services.Keys)
                {
                    var existingService = _services[serviceId];
                    if(existingService == null)
                    {
                        _logger.LogError($"got null service for key {serviceId}");
                        continue;
                    }
                    if(existingService.Expired)
                    {
                        toRemove.Add(serviceId);
                    }
                }

                foreach(var id in toRemove)
                {
                    _services.Remove(id);
                }
                
                if(serviceInfo.ServiceId.HasValue && _services.TryGetValue(serviceInfo.ServiceId.Value, out var service))
                {
                    service.ServiceId = serviceInfo.ServiceId;
                    service.ServiceType = serviceInfo.ServiceType;
                    service.UserId = serviceInfo.UserId;
                    service.ExpiryTime = DateTime.UtcNow.AddMinutes(10);
                    return serviceInfo.ServiceId.Value;
                }

                if(!serviceInfo.ServiceId.HasValue)
                {
                    serviceInfo.ServiceId = GetNextServiceId();
                    _logger.LogInformation($"GetNextServiceId returned {serviceInfo.ServiceId}");
                }

                _services.Add(serviceInfo.ServiceId.Value, new Service()
                {
                    ServiceId = serviceInfo.ServiceId,
                    ServiceType = serviceInfo.ServiceType,
                    UserId = serviceInfo.UserId,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(10)
                });

                return serviceInfo.ServiceId.Value;
            }
        }

        byte _nextServiceId = 0;

        readonly object _lock = new object();

        byte GetNextServiceId()
        {
            lock(_lock)
            {
                byte last = (byte)(_nextServiceId - 1);
                while(_nextServiceId != last)
                {
                    if(!_services.ContainsKey(_nextServiceId))
                    {
                        var serviceId = _nextServiceId;
                        _nextServiceId++;
                        return serviceId;
                    }
                    _nextServiceId++;
                }
                throw new InvalidOperationException("Ran out of Service Ids");
            }
        }

        public IEnumerable<ServiceInfo> Services => 
            _services.Values.Select(s => new ServiceInfo()
            {
                UserId = s.UserId,
                ServiceType = s.ServiceType,
                ServiceId = s.ServiceId
            });
    }
}