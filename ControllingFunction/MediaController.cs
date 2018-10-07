using System.Net;

namespace Ropu.ControllingFunction
{
    public class MediaController
    {
        public MediaController(IPEndPoint controlEndPoint, IPEndPoint mediaEndPoint)
        {
            ControlEndPoint = controlEndPoint;
            MediaEndPoint = mediaEndPoint;
        }

        public IPEndPoint ControlEndPoint
        {
            get;
        }

        public IPEndPoint MediaEndPoint
        {
            get;
        }
    }
}