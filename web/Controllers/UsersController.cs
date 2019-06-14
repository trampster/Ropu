using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles="Admin,User")]
        public IEnumerable<IUser> Users()
        {
            return _usersService.Users;
        }

        [HttpGet("[action]")]
        [Authorize(Roles="Admin,User")]
        public IUser Current()
        {
            uint userId = uint.Parse(base.User.Claims.Where(claim => claim.Type == ClaimTypes.NameIdentifier).Single().Value);
            return _usersService.Get(userId);
        }

        [HttpGet("{userId}")]
        [Authorize(Roles="Admin,User")]
        public IUser UserById(uint userId)
        {
            return _usersService.Get(userId);
        }

        [AllowAnonymous]  
        [HttpPost("[action]")]  
        public IActionResult Create([FromBody]NewUser login)  
        {  
            (bool result, string message) = _usersService.AddUser(login.Name, login.Email, login.Password, new []{"User", "Admin"});
            if(result)
            {
                return Ok();
            }
            return BadRequest(message);  
        } 
    }
}
