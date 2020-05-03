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
    }
}
