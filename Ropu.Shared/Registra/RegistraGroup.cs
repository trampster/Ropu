using System.Collections.Generic;
using System.Net;

namespace Ropu.Shared.Registra
{
    public class RegistraGroup
    {
        public RegistraGroup(ushort groupId)
        {
            GroupId = groupId;
            RegisteredGroupMembers = new List<uint>();
        }

        public ushort GroupId
        {
            get;
        }

        public void Add(uint userId)
        {
            RegisteredGroupMembers.Add(userId);
        }

        public List<uint> RegisteredGroupMembers
        {
            get;
            set;
        }
    }
}