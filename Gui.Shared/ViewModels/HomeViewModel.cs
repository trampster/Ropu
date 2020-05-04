using System.Windows.Input;
using Ropu.Gui.Shared.Services;

namespace Ropu.Gui.Shared.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        readonly INavigator _navigator;

        public HomeViewModel(INavigator navigator)
        {
            _navigator = navigator;
        }

        public ICommand ShowPttView => new AsyncCommand(async () => await _navigator.ShowPttView());

        public ICommand ShowBrowseGroupsView => new AsyncCommand(async () => await _navigator.Show<BrowseGroupsViewModel>());

        public ICommand ShowAboutView => new AsyncCommand(async () => await _navigator.Show<BrowseGroupsViewModel>());
    }
}
