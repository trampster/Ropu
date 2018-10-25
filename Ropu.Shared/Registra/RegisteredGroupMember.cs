using System.Net;

namespace Ropu.Shared.Registra
{
    public class RegisteredGroupMember
    {
        public RegisteredGroupMember(uint unitId, IPEndPoint ipEndPoint)
        {
            UnitId = unitId;
            IPEndPoint = ipEndPoint;
        }
        
        public uint UnitId
        {
            get;
        }

        public IPEndPoint IPEndPoint
        {
            get;            
        }
    }
}