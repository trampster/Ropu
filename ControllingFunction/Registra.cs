using System;
using System.Collections.Generic;
using System.Net;

namespace Ropu.ControllingFunction
{
    public class Registra
    {
        readonly Dictionary<uint, Registration> _registrationLookup = new Dictionary<uint, Registration>();

        public void Register(Registration registration)
        {
            if(_registrationLookup.TryGetValue(registration.UserId, out Registration existing))
            {
                //update 
                _registrationLookup[registration.UserId] = registration;
                return;
            }
            _registrationLookup.Add(registration.UserId, registration);
        }

        public IPEndPoint GetEndPoint(uint userId)
        {
            if(!_registrationLookup.TryGetValue(userId, out Registration registration))
            {
                return null;
            }
            return registration.ControlEndpoint;
        }
    }
}