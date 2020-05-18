using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Ropu.Gui.Shared.Services;
using Ropu.Gui.Shared.ViewModels;

namespace Ropu.ClientUI
{
    public class Navigator : INavigator
    {
        readonly Dictionary<Type, Func<object?, Control>> _viewLookup;
        readonly Stack<Action> _backStack = new Stack<Action>();

        Action<Control> _changeSubView = control => {};
        Action<Control> _changeModalView = control => {};

        public Navigator()
        {
            _viewLookup = new Dictionary<Type, Func<object?, Control>>();
        }

        public async Task Show<T>()
        {
            if(!_viewLookup.TryGetValue(typeof(T), out var viewFactory))
            {
                throw new Exception($"No view registered for type {typeof(T)}");
            }
            var factory = _viewLookup[typeof(T)];
            Control view = factory(null);
            _changeSubView(view);

            await Task.CompletedTask;
        }

        public void Register<ViewModel, View>(Func<View> baseViewModelFactory) where View : Control
        {
            _viewLookup.Add(typeof(ViewModel), arg => baseViewModelFactory());
        }

        public void Register<ViewModel, View, TArg>(Func<TArg, View> baseViewModelFactory) where View : Control
        {
            _viewLookup.Add(typeof(ViewModel), arg => 
            {
                if(arg == null) throw new NullReferenceException();
                return baseViewModelFactory((TArg)arg);
            });
        }

        public void SetModalViewChangeHandler(Action<Control> action)
        {
            _changeModalView = action;
        }

        Func<Control>? _currentModalViewGetter;
        public void SetModalCurrentViewGetter(Func<Control> getCurrentView)
        {
            _currentModalViewGetter = getCurrentView;
        }

        public async Task ShowModal<ViewModelT, ParamT>(ParamT? param) where ParamT : class
        {
            if(!_viewLookup.TryGetValue(typeof(ViewModelT), out var viewFactory))
            {
                throw new Exception($"No view registered for type {typeof(ViewModelT)}");
            }
            var view = _viewLookup[typeof(ViewModelT)](param);
            if(_currentModalViewGetter != null)
            {
                var currentView = _currentModalViewGetter();
                _backStack.Push(() => _changeModalView(currentView));
            }
            _changeModalView(view);

            await Task.CompletedTask;
        }

        public async Task ShowModal<T>()
        {
            await ShowModal<T,object>(null);
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

        public void SetSubViewChangeHandler(Action<Control> action)
        {
            _changeSubView = action;
        }

        public async Task ShowPttView()
        {
            await Show<PttViewModel<Color>>();
        }
    }
}