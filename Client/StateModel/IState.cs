using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ropu.Client.StateModel
{
    public interface IState<Id, EventT>
    {
        Func<CancellationToken, Task> Entry {get;}
        Action<IState<Id, EventT>> Exit {get;}

        Id Identifier {get;}

        IState<Id, EventT> Transition(EventT eventType);

        void AddTransition(EventT eventId, Func<IState<Id, EventT>> getState);

        void RunEntry();

        void RunExit(IState<Id, EventT> newState);
    }
}