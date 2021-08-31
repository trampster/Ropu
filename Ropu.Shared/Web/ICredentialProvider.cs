namespace Ropu.Shared.Web
{
    public interface ICredentialsProvider
    {
        string Email
        {
            get;
            set;
        }

        string Password
        {
            get;
            set;
        }
    }
}