using System;
using System.Collections.Generic;

namespace Ropu.Client.StateModel
{
    public interface IState<Id, EventT>
    {
        Action Entry {get;}
        Action Exit {get;}

        Id Identifier {get;}

        IState<Id, EventT> Transition(EventT eventType);

        void AddTransition(EventT eventId, Func<IState<Id, EventT>> getState);
    }
}