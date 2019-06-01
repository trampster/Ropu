using System.Collections.Generic;

namespace Ropu.Web.Models
{
    public class UserCredentials
    {
        public int Id
        {
            get;
            set;
        }

        public string UserName
        {
            get;
            set;
        }

        public string PasswordHash
        {
            get;
            set;
        }

        public List<string> Roles
        {
            get;
            set;
        }
    }
}