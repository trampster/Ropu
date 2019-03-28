using System.Net;

namespace Ropu.ServingNode
{
    public class UserIPEndPoint : IPEndPoint
    {
        readonly uint _userId;

        readonly int _hashCode;
        public UserIPEndPoint(uint userId, IPEndPoint endpoint)
            : base(endpoint.Address, endpoint.Port)
        {
            _userId = userId;
            _hashCode = (int)(base.GetHashCode() | (int)_userId);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object comparand)
        {
            var other = comparand as UserIPEndPoint;
            if(other == null)
            {
                IPEndPoint otherEndpoint = comparand as IPEndPoint;
                if(otherEndpoint == null)
                {
                    return false;
                }
                return base.Equals(comparand);
            }
            if(_userId != other._userId)
            {
                return false;
            }
            return base.Equals(comparand);
        }
    }
}