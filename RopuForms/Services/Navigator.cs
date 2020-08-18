using Ropu.Gui.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RopuForms.Services
{
    public class Navigator : INavigationService
    {
        readonly Dictionary<Type, Func<object?, Page>> _viewLookup = new Dictionary<Type, Func<object?, Page>>();
        Page _rootPage;

        public void Register<VM,V>(Func<V> baseViewModelFactory) where V : Page
        {
            _viewLookup.Add(typeof(VM), arg => baseViewModelFactory());
        }

        public void Register<ViewModel, View, TArg>(Func<TArg, View> baseViewModelFactory) where View : Page
        {
            _viewLookup.Add(typeof(ViewModel), arg =>
            {
                if (arg == null) throw new NullReferenceException();
                return baseViewModelFactory((TArg)arg);
            });
        }

        public void AddRootPage(Page page)
        {
            _rootPage = page;
        }

        public async Task Show<T>()
        {
            await Task.CompletedTask; // todo, lookup to menu page
        }

        public async Task ShowModal<T>()
        {
            if (!_viewLookup.TryGetValue(typeof(T), out var viewFactory))
            {
                throw new Exception($"No view registered for type {typeof(T)}");
            }
            var view = _viewLookup[typeof(T)](null);
            await _rootPage.Navigation.PushModalAsync(view, false);
        }

        public async Task ShowModal<ViewModelT, ParamT>(ParamT param) where ParamT : class
        {
            if (!_viewLookup.TryGetValue(typeof(ViewModelT), out var viewFactory))
            {
                throw new Exception($"No view registered for type {typeof(ViewModelT)}");
            }
            var view = _viewLookup[typeof(ViewModelT)](param);
            await _rootPage.Navigation.PushModalAsync(view, false);
        }

        public async Task PopModal()
        {
            await _rootPage.Navigation.PopModalAsync();
        }

        public async Task Back()
        {
            await _rootPage.Navigation.PopModalAsync();
        }

        public async Task ShowPttView()
        {
            await Show<PttViewModel<Color>>();
        }

        readonly Dictionary<string, string> _homeLookupFromViewName = new Dictionary<string, string>();
        readonly Dictionary<string, Func<Page>> _controlLookupFromViewName = new Dictionary<string, Func<Page>>();
        readonly Dictionary<string, Func<Page, Task>> _showViewLookupFromHomeName = new Dictionary<string, Func<Page, Task>>();

        public void RegisterView(string navigatorHome, string viewName, Func<Page> control)
        {
            _homeLookupFromViewName.Add(viewName, navigatorHome);
            _controlLookupFromViewName.Add(viewName, control);
        }

        public async Task Navigate(string viewName, bool addToBackStack = true)
        {
            if (!_controlLookupFromViewName.TryGetValue(viewName, out var controlGetter))
            {
                throw new Exception($"No view registered with name {viewName}");
            }
            if (!_homeLookupFromViewName.TryGetValue(viewName, out var homeName))
            {
                throw new Exception($"No home registered for view name {viewName}");
            }
            if (!_showViewLookupFromHomeName.TryGetValue(homeName, out var showView))
            {
                throw new Exception($"No home registerd with name {homeName}");
            }
            if (addToBackStack)
            {
                _stack.Push(viewName);
            }
            await showView(controlGetter());
        }

        Stack<string> _stack = new Stack<string>();

        public async Task NavigateBack()
        {
            _stack.Pop();
            var backTo = _stack.Peek();
            await Navigate(backTo, false);
        }
    }
}
