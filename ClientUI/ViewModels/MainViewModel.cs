using Ropu.Client;
using System.Threading.Tasks;
using Ropu.Gui.Shared.ViewModels;

namespace Ropu.ClientUI.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        readonly IClientSettings _clientSettings;
        readonly Navigator _navigator;
        
        public MainViewModel(IClientSettings clientSettings, Navigator navigator)
        {
            _clientSettings = clientSettings;
            _navigator = navigator;
        }

        public override async Task Initialize()
        {
            if(_clientSettings.UserId == null)
            {
                await _navigator.ShowModal<LoginViewModel>();
            }
        }
    }
}
