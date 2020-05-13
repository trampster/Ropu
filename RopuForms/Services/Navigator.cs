using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ropu.Gui.Shared.ViewModels;
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

        
    }
}
