using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ropu.Web.Models;
using Ropu.Web.Services;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class GroupsController : Controller
    {
        readonly IGroupsService _groupsService;

        public GroupsController(IGroupsService groupsService)
        {
            _groupsService = groupsService;
        }
        
        [HttpGet("[action]")]
        [Authorize(Roles="Admin,User")]
        public IEnumerable<IGroup> Groups()
        {
            return _groupsService.Groups;
        }

        [HttpGet("{groupId}")]
        [Authorize(Roles="Admin,User")]
        public IGroup GroupById(uint groupId)
        {
            return _groupsService.Get(groupId);
        }

        [HttpPost("[action]")]  
        [Authorize(Roles="Admin,User")]
        public IActionResult Create([FromBody]Group group)  
        { 
            (bool result, string message) = _groupsService.AddGroup(group.Name, group.GroupType);
            if(result)
            {
                return Ok();
            }
            return BadRequest(message);  
        }
    }
}
