using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Ropu.Web.Models;
using Ropu.Web.Services;

namespace web.Controllers
{
    [Route("api/[controller]")]  
    [ApiController]  
    public class LoginController : Controller  
    {  
        readonly IConfiguration _config;
        readonly IUsersService _userService;
  
        public LoginController(IConfiguration config, IUsersService usersService)  
        {  
            _config = config;
            _userService = usersService;  
        }

        [AllowAnonymous]  
        [HttpPost]  
        public IActionResult Login([FromBody]Credentials login)  
        {  
            IActionResult response = Unauthorized();  
            var user = AuthenticateUser(login);  
  
            if (user != null)  
            {  
                var tokenString = GenerateJSONWebToken(user.UserName, user.Roles);  
                response = Ok(new { token = tokenString });  
            }  
  
            return response;  
        }  
  
        string GenerateJSONWebToken(string user, IEnumerable<string> roles)  
        {  
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));  
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);  
  
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user));
            foreach(var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],  
                _config["Jwt:Issuer"],  
                claims,  
                expires: DateTime.Now.AddMinutes(120),  
                signingCredentials: credentials);  

                return new JwtSecurityTokenHandler().WriteToken(token);  
        }  
  
        UserCredentials AuthenticateUser(Credentials login)  
        {
            return _userService.AuthenticateUser(login);
        }  
    }  
}