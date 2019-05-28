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
    public class UsersController : Controller
    {
        readonly IUsersService _usersService;

        public UsersController(IUsersService userService)
        {
            _usersService = userService;
        }
        
        [HttpGet("[action]")]
        public IEnumerable<IUser> Users()
        {
            return _usersService.Users;
        }
    }
}
