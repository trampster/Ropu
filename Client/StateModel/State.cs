using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ropu.Client.StateModel
{
    public class State<Id, EventT> : IState<Id, EventT>
    {
        readonly List<Transition<EventT, IState<Id, EventT>>> _transitions;

        public State(Id identifier)
        {
            _transitions = new List<Transition<EventT, IState<Id, EventT>>>();
            Identifier = identifier;
            Entry = _ => Task.Run(()=>{});
            Exit = () => {};
        }

        public Id Identifier {get;}
        public Func<CancellationToken, Task> Entry {get;set;}
        public Action Exit {get;set;}

        public void AddTransition(EventT eventId, Func<IState<Id, EventT>> getState)
        {
            _transitions.Add(new Transition<EventT, IState<Id, EventT> >(eventId, getState));
        }

        public IState<Id, EventT> Transition(EventT eventType)
        {
            foreach(var transition in _transitions)
            {
                if(transition.Event.Equals(eventType))
                {
                    return transition.State;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return Identifier.ToString();
        }

        public void RunEntry()
        {
            _entryTaskCancellationTokenSource = new CancellationTokenSource();
            _entryTask = Entry(_entryTaskCancellationTokenSource.Token);
        }

        Task _entryTask;
        CancellationTokenSource _entryTaskCancellationTokenSource;

        public void RunExit()
        {
            _entryTaskCancellationTokenSource?.Cancel();
            _entryTask?.Wait();

            _entryTaskCancellationTokenSource = null;
            _entryTask = null;

            Exit();
        }
    }
}