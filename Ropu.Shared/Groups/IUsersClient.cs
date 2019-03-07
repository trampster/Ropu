namespace Ropu.Shared.Groups
{
    public interface IUsersClient
    {
        IUser Get(uint userId);
    }
}