using System.Net;

namespace Ropu.Shared.LoadBalancing
{
    public class GroupCallController
    {
        public GroupCallController(IPEndPoint endPoint, ushort groupId)
        {
            EndPoint = endPoint;
            GroupId = groupId;
        }

        public IPEndPoint EndPoint
        {
            get;
        }

        public ushort GroupId
        {
            get;
        }
    }
}