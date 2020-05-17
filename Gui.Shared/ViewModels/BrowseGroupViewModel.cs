using Ropu.Client;
using Ropu.Gui.Shared.Services;
using Ropu.Shared.Groups;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ropu.Gui.Shared.ViewModels
{
    public class BrowseGroupViewModel : BaseViewModel
    {
        readonly Group _group;
        readonly IGroupsClient _groupsClient;
        readonly IClientSettings _clientSettings;
        readonly INavigator _navigator;

        public BrowseGroupViewModel(Group group, IGroupsClient groupsClient, IClientSettings clientSettings, INavigator navigator)
        {
            _group = group;
            _groupsClient = groupsClient;
            _clientSettings = clientSettings;
            _navigator = navigator;
        }

        public override async Task Initialize()
        {
            if(_clientSettings.UserId != null)
            {
                var groups = await _groupsClient.GetMyGroups(_clientSettings.UserId.Value);
                CanJoin = !groups.Any(group => group == _group.Id);
                CanLeave = !CanJoin;
            }
        }

        public byte[]? GroupImage
        {
            get => _group.Image;
        }

        public string Name
        {
            get => _group.Name;
        }

        bool _canJoin = false;
        public bool CanJoin
        {
            get => _canJoin;
            set => SetProperty(ref _canJoin, value);
        }

        bool _canLeave = false;
        public bool CanLeave
        {
            get => _canLeave;
            set => SetProperty(ref _canLeave, value);
        }

        public ICommand Join => new AsyncCommand(async () =>
        {
            if(_clientSettings.UserId == null) return;
            var result = await _groupsClient.Join(_group.Id, _clientSettings.UserId.Value);
            if(result)
            {
                CanLeave = true;
                CanJoin = false;
            }
        });

        public ICommand Leave => new AsyncCommand(async () =>
        {
            if(_clientSettings.UserId == null) return;
            var result = await _groupsClient.Leave(_group.Id, _clientSettings.UserId.Value);
            if(result)
            {
                CanLeave = false;
                CanJoin = true;
            }
        });

        public ICommand Back => new AsyncCommand(async () =>
        {
            await _navigator.PopModal();
        });
    }
}