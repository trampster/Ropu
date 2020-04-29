using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Ropu.ClientUI.ViewModels;
using Ropu.Gui.Shared.Services;
using Ropu.Gui.Shared.ViewModels;

namespace Ropu.ClientUI
{
    public class Navigator : INavigator
    {
        readonly Dictionary<Type, Func<Control>> _viewLookup;
        readonly Stack<Action> _backStack = new Stack<Action>();

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
            if(_currentViewGetter != null)
            {
                var currentView = _currentViewGetter();
                _backStack.Push(() => _changeView(currentView));
            }
            _changeView(view);

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

        Func<Control>? _currentViewGetter;
        public void SetCurrentViewGetter(Func<Control> getCurrentView)
        {
            _currentViewGetter = getCurrentView;
        }

        public async Task ShowModal<T>()
        {
            await Show<T>();
        }

        public async Task Back()
        {
            if(_backStack.TryPop(out Action? action))
            {
                action();
            }
            await Task.CompletedTask;
        }

        public async Task PopModal()
        {
            await Back();
        }

        public async Task ShowPttView()
        {
            await Show<PttViewModel<Color>>();
        }
    }
}