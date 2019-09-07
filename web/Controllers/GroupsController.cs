using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ropu.Web.Models;
using Ropu.Web.Services;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class GroupsController : Controller
    {
        readonly IGroupsService _groupsService;
        readonly GroupMembersipService _groupMembershipService;
        readonly ILogger _logger;

        public GroupsController(IGroupsService groupsService, GroupMembersipService groupMembersipService, ILogger<GroupsController> logger)
        {
            _groupsService = groupsService;
            _groupMembershipService = groupMembersipService;
            _logger = logger;
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

        [HttpPost("[action]")]
        [Authorize(Roles="Admin,User")]
        public IActionResult Edit([FromBody]Group group)
        {
            (bool result, string message) = _groupsService.Edit(group);
            if(!result)
            {
                return BadRequest(message);
            }
            return Ok();
        }

        [HttpDelete("{id}/Delete")]
        [Authorize(Roles="Admin,User")]
        public IActionResult Delete(uint id)
        {
            (bool result, string message) = _groupsService.Delete(id);
            if(!result)
            {
                return BadRequest(message);
            }
            return Ok();
        }

        [HttpPost("{groupId}/Join/{userId}")]
        [Authorize(Roles="Admin,User")]
        public IActionResult Join(ushort groupId, uint userId)
        {
            (bool result, string message) = _groupMembershipService.AddGroupMember(groupId, userId);
            if(!result)
            {
                _logger.LogError($"Failed to join group with message {message}");
                return BadRequest(message);
            }
            return Ok();
        }

        [HttpGet("{groupId}/Members")]
        public List<IUser> Members(ushort groupId)
        {
            var members = _groupMembershipService.GetGroupMembers(groupId);
            if(members == null)
            {
                var message = "Failed to get group members";
                _logger.LogError(message);
                return null;
            }
            return members;
        }
    }
}
