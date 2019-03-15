namespace Ropu.Shared.Groups
{
    public class User : IUser
    {
        public string Name
        {
            get;
            set;
        }

        public byte[] Image
        {
            get;
            set;
        }
    }
}