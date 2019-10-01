namespace Ropu.Shared.Web
{
    public class CredentialsProvider
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