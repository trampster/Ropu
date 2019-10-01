namespace Ropu.Web.Models
{
    public class EditableUser : RedisUser
    {
        public EditableUser()
            : base()
        {
            Password = "";
        }
        
        public string Password
        {
            get;
            set;
        }
    }
}