using System.Net;

namespace Ropu.Shared.Registra
{
    public class RegistraUser
    {
        public RegistraUser(uint userId, IPEndPoint endPoint)
        {
            UserId = userId;
            EndPoint = endPoint;
        }

        public uint UserId
        {
            get;
        }

        public IPEndPoint EndPoint
        {
            get;
        }
    }
}