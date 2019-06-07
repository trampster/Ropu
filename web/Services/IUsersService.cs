using System.Collections.Generic;
using Ropu.Web.Models;

namespace Ropu.Web.Services
{
    public interface IUsersService
    {
        IEnumerable<IUser> Users
        {
            get;
        }


        UserCredentials AuthenticateUser(Credentials credentials);

        bool AddUser(string name, string username, string password, string[] roles);
    }
}