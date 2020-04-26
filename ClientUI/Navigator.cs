using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eto.Forms;
using Ropu.Gui.Shared.Services;

namespace Ropu.ClientUI
{
    public class Navigator : INavigator
    {
        readonly Dictionary<Type, Func<Control>> _viewLookup;
        readonly Queue<Action> _backQueue = new Queue<Action>();

        Action<Control> _changeView = control => {};

        public Navigator()
        {
            _viewLookup = new Dictionary<Type, Func<Control>>();
        }

        public async Task Show<T>()
        {
            if(!_viewLookup.TryGetValue(typeof(T), out var viewFactory))
            {
                throw new Exception($"No view registered for type {typeof(T)}");
            }
            var view = _viewLookup[typeof(T)]();
            _changeView(view);

            _backQueue.Enqueue(() => _changeView(view));
            await Task.CompletedTask;
        }

        public void Register<ViewModel, View>(Func<View> baseViewModelFactory) where View : Control
        {
            _viewLookup.Add(typeof(ViewModel), baseViewModelFactory);
        }

        public void SetViewChangeHandler(Action<Control> action)
        {
            _changeView = action;
        }


        public async Task ShowModal<T>()
        {
            await Show<T>();
        }

        public async Task Back()
        {
            if(_backQueue.TryDequeue(out Action? action))
            {
                action();
            }
            await Task.CompletedTask;
        }

        public async Task PopModal()
        {
            await Back();
        }
    }
}