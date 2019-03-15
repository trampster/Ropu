namespace Ropu.Shared.Groups
{
    public interface IUser
    {
        string Name
        {
            get;
        }

        byte[] Image
        {
            get;
        }
    }
}