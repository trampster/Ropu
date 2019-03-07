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
                default:
                    return new User()
                    {
                        Name = "unknown"
                    };
            }
        }
    }
}