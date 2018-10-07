using System.Net;

namespace Ropu.ControllingFunction
{
    public class FloorController
    {
        public FloorController(IPEndPoint controlEndPoint, IPEndPoint floorEndPoint)
        {
            ControlEndPoint = controlEndPoint;
            FloorEndPoint = floorEndPoint;
        }

        public IPEndPoint ControlEndPoint
        {
            get;
        }

        public IPEndPoint FloorEndPoint
        {
            get;
        }
    }
}