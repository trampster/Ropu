using System.Net;

namespace Ropu.Shared.LoadBalancing
{
    public class GroupCallController
    {
        public IPEndPoint EndPoint
        {
            get;
            set;
        }

        public ushort GroupId
        {
            get;
            set;
        }
    }
}