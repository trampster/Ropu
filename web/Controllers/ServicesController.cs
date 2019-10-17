using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ropu.Shared;
using Ropu.Shared.WebModels;
using Ropu.Web.Services;

namespace Ropu.Web.Controllers
{
    [Route("api/[controller]")]  
    [ApiController]  
    public class ServicesController : Controller
    {
        readonly ServicesService _servicesService;

        public ServicesController(ServicesService servicesService)
        {
            _servicesService = servicesService;
        }

        [HttpPost("[action]")]
        [Authorize(Roles="Service")]
        public IActionResult UpdateLoadBalancer([FromBody]LoadBalancerInfo loadBalancerInfo)
        {
            _servicesService.LoadBalancerInfo = loadBalancerInfo;
            return Ok();
        }

        [HttpGet("[action]")]
        [Authorize(Roles="Admin")]
        public LoadBalancerInfo? LoadBalancer([FromBody]LoadBalancerInfo loadBalancerInfo)
        {
            return _servicesService.LoadBalancerInfo;
        }

        [HttpGet("[action]")]
        [Authorize(Roles="Admin,User")]
        public string LoadBalancerIPEndpoint()
        {
            return (_servicesService?.LoadBalancerInfo?.IPEndPoint).EmptyIfNull();
        }

        [HttpPost("[action]")]
        [Authorize(Roles="Service")]
        public byte Register([FromBody]ServiceInfo serviceInfo)
        {
            return _servicesService.RegisterService(serviceInfo);
        }

        [HttpGet("[action]")]
        [Authorize(Roles="Admin,User,Service")]
        public IEnumerable<ServiceInfo> All()
        {
            return _servicesService.Services;
        }
    }
}