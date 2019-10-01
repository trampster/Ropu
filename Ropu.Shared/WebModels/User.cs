namespace Ropu.Shared.WebModels
{
    public class User : IUser
    {
        public User()
        {
            Name = "";
            ImageHash = "";
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
    }
}