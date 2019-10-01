using System.Collections.Generic;
using Ropu.Shared.WebModels;

namespace Ropu.Web.Models
{
    public class RedisUser : IUser
    {
        public RedisUser()
        {
            Name = "";
            Email = "";
            ImageHash = "";
            PasswordHash = "";
            Roles = new List<string>();
        }

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