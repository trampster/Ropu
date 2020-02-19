using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ropu.Shared.WebModels;
using Ropu.Web.Services;

namespace Ropu.Web.Controllers
{
    [Route("api/[controller]")]  
    [ApiController]  
    public class KeyController : Controller
    {
        readonly KeyService _keyService;
        readonly ServicesService _servicesService;

        public KeyController(KeyService keyService, ServicesService servicesService)
        {
            _keyService = keyService;
            _servicesService = servicesService;
        }

        [HttpGet("[action]")]
        [Authorize(Roles="Admin,User,Service")]
        public IActionResult MyKeys()
        {
            uint userId = uint.Parse(base.User.Claims.Where(claim => claim.Type == ClaimTypes.NameIdentifier).Single().Value);
            return Ok(_keyService.GetKeys(false, userId));
        }

        [HttpGet("{isGroup}/{groupOrUserId}")]
        [Authorize(Roles="Admin,User,Service")]
        public IActionResult Keys(bool isGroup, uint groupOrUserId)
        {
            if(!isGroup)
            {
                if(User.IsInRole("Admin") || User.IsInRole("Service"))
                {
                    //allowed
                }
                else if(User.IsInRole("User"))
                { 
                    if(User.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier && uint.Parse(claim.Value) == groupOrUserId))
                    {
                        //its there two keys which is fine
                    }
                    else
                    {
                        if(!_servicesService.IsService(groupOrUserId))
                        {
                            //Don't allow users to get other users keys
                            return Forbid();
                        }
                    }
                }
            }
            return Ok(_keyService.GetKeys(isGroup, groupOrUserId));
        }

        [HttpPost("[action]")]
        [Authorize(Roles="Admin,Service")]
        public IEnumerable<EntitiesKeys> UsersKeys([FromBody]List<uint> userIds)
        {
            foreach(var userId in userIds)
            {
                var keys = _keyService.GetKeys(false, userId);
                yield return new EntitiesKeys()
                {
                    UserOrGroupId = userId,
                    Keys = keys
                };
            }
        }
    }
}