using System;
using Eto.Forms;
using Eto.Drawing;
using System.Windows.Input;
using Ropu.Client;
using Ropu.Shared.Groups;
using Ropu.Shared.Web;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Ropu.Shared;

namespace Ropu.ClientUI.ViewModels
{
    public class PttViewModel : BaseViewModel
    {
        readonly RopuClient _ropuClient;
        readonly IClientSettings _clientSettings;
        readonly IGroupsClient _groupsClient;
        readonly IUsersClient _usersClient;
        readonly ImageClient _imageClient;

        public PttViewModel(
            RopuClient ropuClient, 
            IClientSettings clientSettings, 
            IGroupsClient groupsClient, 
            IUsersClient usersClient, 
            ImageClient imageClient)
        {
            _ropuClient = ropuClient;
            _ropuClient.StateChanged += (sender, args) => 
            {
                Application.Instance.Invoke(ChangeState);
            };
            _groupsClient = groupsClient;
            _usersClient = usersClient;
            _imageClient = imageClient;

            _clientSettings = clientSettings;

            _state = _ropuClient.State.ToString();
        }

        public override async Task Initialize()
        {
            _clientSettings.UserId = (await _usersClient.GetCurrentUser())?.Id;

            if(_clientSettings.UserId == null) throw new InvalidOperationException("UserId is not set");

            var groups = (await _groupsClient.GetUsersGroups(_clientSettings.UserId.Value));
            _ropuClient.IdleGroup = groups[0];

            var idleGroup = await _groupsClient.Get(_ropuClient.IdleGroup);
            if(idleGroup != null)
            {
                IdleGroup = idleGroup.Name;
                IdleGroupImage = idleGroup.Image;
            }

            await _ropuClient.Run();
        }

        bool InCall(StateId state)
        {
            switch (state)
            {
                case StateId.InCallIdle:
                case StateId.InCallRequestingFloor:
                case StateId.InCallReceiving:
                case StateId.InCallTransmitting:
                case StateId.InCallReleasingFloor:
                    return true;
                default:
                    return false;
            }
        }

        async Task ChangeState()
        {
            var state = _ropuClient.State;
            State = state.ToString();
            switch (state)
            {
                case StateId.Start:
                case StateId.Unregistered:
                    PttColor = Gray;
                    break;
                case StateId.Registered:
                case StateId.Deregistering:
                case StateId.StartingCall:
                    PttColor = Blue;
                    break;
                case StateId.InCallRequestingFloor:
                case StateId.InCallReleasingFloor:
                case StateId.InCallTransmitting:
                case StateId.InCallIdle:
                case StateId.InCallReceiving:
                    PttColor = Green;
                    break;
                default:
                    throw new Exception("Unhandled Call State");
            }

            Transmitting = state == StateId.InCallTransmitting;

            var callGroup = InCall(state) ? await _groupsClient.Get(_ropuClient.CallGroup) : null;
            CallGroup = callGroup?.Name == null ? "" : callGroup.Name;
            CallGroupImage = callGroup?.Image;

            CircleText = (InCall(state) ? 
                (await _groupsClient.Get(_ropuClient.CallGroup))?.Name : 
                (await _groupsClient.Get(_ropuClient.IdleGroup))?.Name).EmptyIfNull();

            var user = state == StateId.InCallReceiving ? 
                (_ropuClient.Talker.HasValue ? await _usersClient.Get(_ropuClient.Talker.Value) : null) :
                null;
            Talker = user?.Name;
            if(user != null)
            {
                TalkerImage = await _imageClient.GetImage(user.ImageHash);
            }
        }

        string _state = "";
        public string State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        string _pttState = "PTT Up";
        public string PttState
        {
            get => _pttState;
            set => SetProperty(ref _pttState, value);
        }

        string _userId = "";
        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        string _userIdError = "";
        public string UserIdError
        {
            get => _userIdError;
            set => SetProperty(ref _userIdError, value);
        }

        public string GroupId
        {
            get => _ropuClient.IdleGroup.ToString();
            set
            {

                if(!ushort.TryParse(value, out ushort groupId))
                {
                    GroupIdError = "Error";
                }
                GroupIdError = "";
                _ropuClient.IdleGroup = groupId;
                RaisePropertyChanged();
            }
        }

        public readonly static Color Blue = Color.FromRgb(0x3193e3);
        public readonly static Color Green = Color.FromRgb(0x31e393);
        public readonly static Color Gray = Color.FromRgb(0x999999);
        public readonly static Color Red = Color.FromRgb(0xFF6961);


        Color _pttColor = Blue;
        public Color PttColor
        {
            get => _pttColor;
            set => SetProperty(ref _pttColor, value);
        }

        string? _talker = "";
        public string? Talker
        {
            get => _talker;
            set => SetProperty(ref _talker, value);
        }

        byte[]? _talkerImage;
        public byte[]? TalkerImage
        {
            get => _talkerImage;
            set => SetProperty(ref _talkerImage, value);
        }

        bool _transmitting;
        public bool Transmitting
        {
            get => _transmitting;
            set => SetProperty(ref _transmitting, value);
        }

        string _callGroup = "";
        public string CallGroup
        {
            get => _callGroup;
            set => SetProperty(ref _callGroup, value);
        }

        byte[]? _callGroupImage;
        public byte[]? CallGroupImage
        {
            get => _callGroupImage;
            set => SetProperty(ref _callGroupImage, value);
        }

        string _idleGroup = "";
        public string IdleGroup
        {
            get => _idleGroup;
            set => SetProperty(ref _idleGroup, value);
        }

        byte[]? _idleGroupImage;
        public byte[]? IdleGroupImage
        {
            get => _idleGroupImage;
            set => SetProperty(ref _idleGroupImage, value);
        }

        string _circleText = "";
        public string CircleText
        {
            get => _circleText;
            set => SetProperty(ref _circleText, value);
        }

        string _groupIdError = "";
        public string GroupIdError
        {
            get => _groupIdError;
            set => SetProperty(ref _groupIdError, value);
        }

        public ICommand PttDownCommand => new ActionCommand(() => 
        {
            PttState = "PTT Down";
            _ropuClient.PttDown();
        });
        public ICommand PttUpCommand => new ActionCommand(() => 
        {
            PttState = "PTT Up";
            _ropuClient.PttUp();
        });
    }
}
