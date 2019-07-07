using System.Collections.Generic;

namespace Ropu.Web.Models
{
    public class RedisUser : IUser
    {
        public uint Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string ImageHash
        {
            get;
            set;
        }

        public string Email
        {
            get;
            set;
        }

        public List<string> Roles
        {
            get;
            set;
        }

        public string PasswordHash
        {
            get;
            set;
        }
    }
}