using System.Collections.Generic;

namespace Ropu.Web.Models
{
    public class EditableUser : RedisUser
    {
        public string Password
        {
            get;
            set;
        }
    }
}