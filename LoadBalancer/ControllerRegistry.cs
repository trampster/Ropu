using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Ropu.LoadBalancer
{
    public class ControllerRegistry<T> where T : class, IRegisteredController
    {
        readonly ConcurrentDictionary<IPEndPoint, T> _controllers = new ConcurrentDictionary<IPEndPoint, T>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callManagementEndpoint"></param>
        /// <param name="update"></param>
        /// <param name="createContoller"></param>
        /// <returns>true if new else false if update</returns>
        public bool Register(IPEndPoint callManagementEndpoint, Action<T> update, Func<T> createContoller)
        {
            if(_controllers.TryGetValue(callManagementEndpoint, out T? existingMediaController))
            {
                update(existingMediaController);
                return false;
            }
            _controllers.TryAdd(callManagementEndpoint, createContoller());
            return true;
        }

        public T? GetAvailableController()
        {
            foreach(var controllerPair in _controllers)
            {
                if(!controllerPair.Value.IsExpired())
                {
                    return controllerPair.Value;
                }
            }
            return null;
        }

        public void RemoveExpired(Action<T> onRemove)
        {
            foreach(var controllerPair in _controllers)
            {
                if(controllerPair.Value.IsExpired())
                {
                    Console.WriteLine("Serving Node Expired");
                    if(_controllers.TryRemove(controllerPair.Key, out T? value))
                    {
                        onRemove(controllerPair.Value);
                    }
                }
            }
        }

        public IEnumerable<IPEndPoint> GetLoadBalancerEndPoints()
        {
            return _controllers.Keys;
        }

        public IEnumerable<T> GetControllers()
        {
            return _controllers.Values;
        }

        public int Count => _controllers.Count;
    }
}