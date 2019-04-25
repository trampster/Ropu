using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ropu.Web.Models;
using Ropu.Web.Services;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class GroupsController : Controller
    {
        readonly IGroupsService _groupsService;

        public GroupsController()
        {
            _groupsService = new HardcodedGroupsService();
        }
        
        [HttpGet("[action]")]
        public IEnumerable<IGroup> Groups()
        {
            return _groupsService.Groups;
        }
    }
}
