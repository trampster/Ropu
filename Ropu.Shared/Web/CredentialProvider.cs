namespace Ropu.Shared.Web
{
    public class CredentialsProvider : ICredentialsProvider
    {
        public CredentialsProvider()
        {
            Email = "";
            Password = "";
        }

        public string Email
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }
    }
}