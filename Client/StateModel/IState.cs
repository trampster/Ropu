using System;
using System.Collections.Generic;

namespace Ropu.Client.StateModel
{
    public interface IState<EventT>
    {
        Action Entry {get;}
        Action Exit {get;}

        IState<EventT> Transition(EventT eventType);
    }
}