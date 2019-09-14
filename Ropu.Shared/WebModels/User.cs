namespace Ropu.Shared.WebModels
{
    public class User : IUser
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
    }
}