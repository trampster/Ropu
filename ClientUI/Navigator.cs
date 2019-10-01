using System;
using System.Collections.Generic;
using Eto.Forms;

namespace Ropu.ClientUI
{
    public class Navigator
    {
        readonly Dictionary<Type, Func<Control>> _viewLookup;

        Action<Control> _changeView = control => {};

        public Navigator()
        {
            _viewLookup = new Dictionary<Type, Func<Control>>();
        }

        public void Show<T>()
        {
            if(!_viewLookup.TryGetValue(typeof(T), out var viewFactory))
            {
                throw new Exception($"No view registered for type {typeof(T)}");
            }
            var view = _viewLookup[typeof(T)]();
            _changeView(view);
        }

        public void Register<T>(Func<T> baseViewModelFactory) where T : Control
        {
            _viewLookup.Add(typeof(T), baseViewModelFactory);
        }

        public void RegisterSingleton<T>(T baseViewModel) where T : Control
        {
            _viewLookup.Add(typeof(T), () => baseViewModel);
        }

        public void SetViewChangeHandler(Action<Control> action)
        {
            _changeView = action;
        }
    }
}