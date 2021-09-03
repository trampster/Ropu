using System.Threading.Tasks;
using Ropu.Client;
using Ropu.Gui.Shared.ViewModels;
using RopuForms.Services;

namespace RopuForms.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        readonly ISettingsManager _settingsManager;
        readonly INavigationService _navigator;

        public MainViewModel(ISettingsManager settingsManager, INavigationService navigator)
        {
            _settingsManager = settingsManager;
            _navigator = navigator;
        }

        public override async Task Initialize()
        {
            if (_settingsManager.ClientSettings?.UserId == null)
            {
                await _navigator.ShowModal<LoginViewModel>();
            }
        }
    }
}
