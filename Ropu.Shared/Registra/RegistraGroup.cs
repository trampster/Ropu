using System.Collections.Generic;
using System.Net;

namespace Ropu.Shared.Registra
{
    public class RegistraGroup
    {
        public RegistraGroup(ushort groupId)
        {
            GroupId = groupId;
            RegisteredGroupMember = new List<RegisteredGroupMember>();
            EndPoints = new List<IPEndPoint>();
        }

        public ushort GroupId
        {
            get;
        }

        public void Add(uint userId, IPEndPoint endpoint)
        {
            RegisteredGroupMember.Add(new RegisteredGroupMember(userId, endpoint));
            EndPoints.Add(endpoint);
        }

        public List<RegisteredGroupMember> RegisteredGroupMember
        {
            get;
        }

        public List<IPEndPoint> EndPoints
        {
            get;
        }
    }
}