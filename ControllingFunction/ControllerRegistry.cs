using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Ropu.ControllingFunction
{
    public class ControllerRegistry<T> where T : class, IRegisteredController
    {
        readonly ConcurrentDictionary<IPEndPoint, T> _controllers = new ConcurrentDictionary<IPEndPoint, T>();

        public void Register(IPEndPoint callManagementEndpoint, Action<T> update, Func<T> createContoller)
        {
            if(_controllers.TryGetValue(callManagementEndpoint, out T existingMediaController))
            {
                update(existingMediaController);
            }
            else
            {
                _controllers.TryAdd(callManagementEndpoint, createContoller());
            }
        }

        public T GetAvailableController()
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

        public IEnumerable<IPEndPoint> GetEndPoints()
        {
            return _controllers.Keys;
        }

    }
}