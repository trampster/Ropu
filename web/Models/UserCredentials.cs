using System.Collections.Generic;

namespace Ropu.Web.Models
{
    public class UserCredentials
    {
        public uint Id
        {
            get;
            set;
        }

        public string Email
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