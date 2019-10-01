using System.Collections.Generic;

namespace Ropu.Web.Models
{
    public class NewUser
    {
        public NewUser()
        {
            Email = "";
            Name = "";
            Password = "";
        }

        public string Email
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }
    }
}