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
            var user = _usersService.GetFull(userId);
            user.Password = null;
            user.PasswordHash = null;
            if(!CanEdit(userId))
            {
                user.Email = null;
                user.Roles = null;
            }
            return user;
        }

        [HttpGet("{userId}/CanEdit")]
        [Authorize(Roles="Admin,User")]
        public bool CanEdit(uint userId)
        {
            if(this.User.IsInRole("Admin"))
            {
                return true;
            }
            if(this.User.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == userId.ToString()))
            {
                return true;
            }
            return false;
        }

        [AllowAnonymous]  
        [HttpPost("[action]")]  
        public IActionResult Create([FromBody]NewUser login)  
        { 
            var roles = new List<string>();
            roles.Add("User");
            if(_usersService.Count() == 0)
            {
                roles.Add("Admin");
            }
            (bool result, string message) = _usersService.AddUser(login.Name, login.Email, login.Password, roles);
            if(result)
            {
                return Ok();
            }
            return BadRequest(message);  
        }

        [HttpPost("[action]")]
        [Authorize(Roles="Admin,User")]
        public IActionResult Edit([FromBody]EditableUser user)
        {
            if(!CanEdit(user.Id))
            {
                return Forbid();
            }
            (bool result, string message) =_usersService.Edit(user);
            if(!result)
            {
                return BadRequest(message);
            }
            return Ok();
        }
    }
}
