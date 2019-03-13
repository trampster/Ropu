namespace Ropu.Shared.Groups
{
    public class HardcodedUsersClient : IUsersClient
    {
        public IUser Get(uint userId)
        {
            switch(userId)
            {
                case 1004:
                    return new User()
                    {
                        Name = "Iron Man"
                    };
                case 1005:
                    return new User()
                    {
                        Name = "Hulk"
                    };
                case 2004:
                    return new User()
                    {
                        Name = "Batman"
                    };
                case 2005:
                    return new User()
                    {
                        Name = "Superman"
                    };
                case 2000:
                    return new User()
                    {
                        Name = "Deadpool"
                    };
                default:
                    return new User()
                    {
                        Name = "unknown"
                    };
            }
        }
    }
}