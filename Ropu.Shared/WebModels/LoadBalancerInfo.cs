namespace Ropu.Shared.WebModels
{
    public class LoadBalancerInfo
    {
        public string IPEndPoint
        {
            get;
            set;
        }

        public int RegisteredServingNodes
        {
            get;
            set;
        }

        public int RegisteredCallControllers
        {
            get;
            set;
        }
    }
}