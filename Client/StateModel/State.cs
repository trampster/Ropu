using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ropu.Client.StateModel
{
    public class State<Id, EventT> : IState<Id, EventT> where EventT : struct where Id : struct
    {
        readonly List<Transition<EventT, IState<Id, EventT>>> _transitions;

        public State(Id identifier)
        {
            _transitions = new List<Transition<EventT, IState<Id, EventT>>>();
            Identifier = identifier;
            Entry = _ => Task.Run(()=>{});
            Exit = _ => {};
        }

        public Id Identifier {get;}
        public Func<CancellationToken, Task> Entry {get;set;}
        public Action<IState<Id, EventT>> Exit {get;set;}

        public void AddTransition(EventT eventId, Func<IState<Id, EventT>> getState)
        {
            _transitions.Add(new Transition<EventT, IState<Id, EventT> >(eventId, getState));
        }

        public void AddTransitions(IEnumerable<EventT> eventIds, Func<IState<Id, EventT>> getState)
        {
            foreach(var eventId in eventIds)
            {
                _transitions.Add(new Transition<EventT, IState<Id, EventT> >(eventId, getState));
            }
        }

        public IState<Id, EventT> Transition(EventT eventType)
        {
            foreach(var transition in _transitions)
            {
                if(transition != null && transition.Event.Equals(eventType))
                {
                    return transition.State;
                }
            }
            throw new InvalidOperationException($"Could not find a transition for event {eventType} in state {this.Identifier}");
        }

        public override string? ToString()
        {
            return Identifier.ToString();
        }

        public void RunEntry()
        {
            _entryTaskCancellationTokenSource = new CancellationTokenSource();
            _entryTask = Entry(_entryTaskCancellationTokenSource.Token);
        }

        Task? _entryTask;
        CancellationTokenSource? _entryTaskCancellationTokenSource;

        public async void RunExit(IState<Id, EventT> newState)
        {
            _entryTaskCancellationTokenSource?.Cancel();
            if(_entryTask != null)
            {
                await _entryTask;
            }

            _entryTaskCancellationTokenSource = null;
            _entryTask = null;

            Exit(newState);
        }

        public bool HasTransition(EventT eventType)
        {
            return _transitions.Any(t => t.Event.Equals(eventType));
        }
    }
}