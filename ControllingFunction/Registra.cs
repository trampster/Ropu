using System;
using System.Collections.Generic;

namespace Ropu.ContollingFunction
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
    }
}