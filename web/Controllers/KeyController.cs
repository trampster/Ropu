using System.Collections.Generic;
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

        [HttpGet("{isGroup}/{groupOrUserId}")]
        [Authorize(Roles="Admin,User")]
        public List<EncryptionKey> Keys(bool isGroup, uint groupOrUserId)
        {
            return _keyService.GetKeys(isGroup, groupOrUserId);
        }

        [HttpGet("[action]")]
        [Authorize(Roles="Admin,User,Service")]
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