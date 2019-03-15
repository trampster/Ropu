using System;
using System.IO;

namespace Ropu.Shared.Groups
{
    public class HardcodedUsersClient : IUsersClient
    {
        IUser Create(string name)
        {
            return new User()
            {
                Name = name,
                Image = GetImage(name)
            };
        }

        byte[] GetImage(string name)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var iconPath = Path.Combine(home, "RopuIcons", $"{name}.png");
            if(File.Exists(iconPath))
            {
                return File.ReadAllBytes(iconPath);
            }
            return  File.ReadAllBytes("../Icon/rope32.png");
        }

        public IUser Get(uint userId)
        {
            switch(userId)
            {
                case 1004:
                    return Create("Iron Man");
                case 1005:
                    return Create("Hulk");
                case 2004:
                    return Create("Batman");
                case 2005:
                    return Create("Superman");
                case 2000:
                    return Create("Deadpool");
                default:
                    return Create("unknown");
            }
        }
    }
}