namespace Ropu.Web.Models
{
    public class User : IUser
    {
        public int Id
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
    }
}