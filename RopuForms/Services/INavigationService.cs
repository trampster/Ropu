using System;
using Ropu.Gui.Shared.Services;
using Xamarin.Forms;

namespace RopuForms.Services
{
    public interface INavigationService : INavigator
    {
        void Register<VM,V>(Func<V> baseViewModelFactory) where V : Page;

        void Register<ViewModel, View, TArg>(Func<TArg, View> baseViewModelFactory) where View : Page;

        void AddRootPage(Page page);
    }
}
