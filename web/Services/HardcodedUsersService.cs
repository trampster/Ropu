using System.Collections.Generic;
using Ropu.Web.Models;

namespace Ropu.Web.Services
{
    public class HardcodedUsersService : IUsersService
    {
        readonly IUser[] _users = new[]
        {
            new User()
            {
                Name = "Batmap",
                Id = 1
            },
            new User()
            {
                Name = "Superman",
                Id = 2
            }
        };

        public IEnumerable<IUser> Users
        {
            get
            {
                return _users;
            }
        }

        public (bool, string) AddUser(string email, string username, string password, string[] roles)
        {
            throw new System.NotImplementedException();
        }

        public UserCredentials AuthenticateUser(Credentials credentials)
        {
            return new UserCredentials()
            {
                Email = credentials.Email,
                Roles = new List<string>{"Admin"}
            };
        }
    }
}