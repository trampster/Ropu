using Ropu.Client;
using Ropu.Shared.Groups;
using System.Linq;
using System.Threading.Tasks;

namespace Ropu.Gui.Shared.ViewModels
{
    public class BrowseGroupViewModel : BaseViewModel
    {
        readonly Group _group;
        readonly GroupsClient _groupsClient;
        readonly IClientSettings _clientSettings;

        public BrowseGroupViewModel(Group group, GroupsClient groupsClient, IClientSettings clientSettings)
        {
            _group = group;
            _groupsClient = groupsClient;
            _clientSettings = clientSettings;
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
    }
}