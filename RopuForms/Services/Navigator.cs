using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RopuForms.Services
{
    public class Navigator : INavigationService
    {
        readonly Dictionary<Type, Func<Page>> _viewLookup = new Dictionary<Type, Func<Page>>();
        Page _rootPage;

        public void Register<VM,V>(Func<V> baseViewModelFactory) where V : Page
        {
            _viewLookup.Add(typeof(VM), baseViewModelFactory);
        }

        public void AddRootPage(Page page)
        {
            _rootPage = page;
        }

        public async Task ShowModal<T>()
        {
            if (!_viewLookup.TryGetValue(typeof(T), out var viewFactory))
            {
                throw new Exception($"No view registered for type {typeof(T)}");
            }
            var view = _viewLookup[typeof(T)]();
            await _rootPage.Navigation.PushModalAsync(view, false);
        }

        public async Task Back()
        {
            await _rootPage.Navigation.PopModalAsync();
        }
    }
}
