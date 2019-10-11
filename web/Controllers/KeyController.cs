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

        public KeyController(KeyService keyService)
        {
            _keyService = keyService;
        }

        [HttpGet("{isGroup}/{groupOrUserId}")]
        [Authorize(Roles="Admin,User")]
        public List<EncryptionKey> Keys(bool isGroup, uint groupOrUserId)
        {
            return _keyService.GetKeys(isGroup, groupOrUserId);
        }
    }
}