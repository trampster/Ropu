using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Ropu.Web.Models;

namespace web.Controllers
{
    [Route("api/[controller]")]  
    [ApiController]  
    public class LoginController : Controller  
    {  
        private IConfiguration _config;  
  
        public LoginController(IConfiguration config)  
        {  
            _config = config;  
        }

        [AllowAnonymous]  
        [HttpPost]  
        public IActionResult Login([FromBody]Credentials login)  
        {  
            IActionResult response = Unauthorized();  
            var user = AuthenticateUser(login);  
  
            if (AuthenticateUser(login))  
            {  
                var tokenString = GenerateJSONWebToken(login.UserName, "Admin");  
                response = Ok(new { token = tokenString });  
            }  
  
            return response;  
        }  
  
        private string GenerateJSONWebToken(string user, string role)  
        {  
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));  
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);  
  

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],  
                _config["Jwt:Issuer"],  
                new Claim[]
                {
                    new Claim(ClaimTypes.Role, role),
                    new Claim(ClaimTypes.NameIdentifier, user),
                },  
                expires: DateTime.Now.AddMinutes(120),  
                signingCredentials: credentials);  

                return new JwtSecurityTokenHandler().WriteToken(token);  
        }  
  
        bool AuthenticateUser(Credentials login)  
        {
            if (login.UserName == "User1" && login.Password == "password1")
            {
                return true;
            }
            return false;
        }  
    }  
}