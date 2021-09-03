using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Ropu.Client;
using Ropu.Gui.Shared.Services;
using Ropu.Shared;
using Ropu.Shared.Groups;
using Ropu.Shared.Web;

namespace Ropu.Gui.Shared.ViewModels
{
    public class PttViewModel<ColorT> : BaseViewModel
    {
        readonly RopuClient _ropuClient;
        readonly ISettingsManager _settingsManager;
        readonly IGroupsClient _groupsClient;
        readonly IUsersClient _usersClient;
        readonly ImageClient _imageClient;
        readonly IColorService<ColorT> _colorService;
        readonly IPermissionService _permissionService;
        readonly RopuWebClient _webClient;
        readonly INavigator _navigator;

        public PttViewModel(
            RopuClient ropuClient,
            ISettingsManager settingsManager, 
            IGroupsClient groupsClient, 
            IUsersClient usersClient, 
            ImageClient imageClient,
            IColorService<ColorT> colorService,
            Action<Func<Task>> invoke,
            IPermissionService permissionService,
            RopuWebClient webClient,
            INavigator navigator)
        {
            _navigator = navigator;
            _ropuClient = ropuClient;
            _webClient = webClient;

            _ropuClient.StateChanged += (sender, args) => 
            {
                invoke(ChangeState);
            };
            _ropuClient.IdleGroupChanged += async (sender, args) =>
            {
                await UpdateIdleGroup();
                await ChangeState();
            };
            _groupsClient = groupsClient;
            _usersClient = usersClient;
            _imageClient = imageClient;

            _settingsManager = settingsManager;
            _colorService = colorService;
            _permissionService = permissionService;

            Blue = _colorService.FromRgb(0x3193e3);
            Green = _colorService.FromRgb(0x31e393);
            Gray = _colorService.FromRgb(0x999999);
            Red = _colorService.FromRgb(0xFF6961);

            _pttColor = Gray;
            _receivingColor = Red;
            _transmittingColor = Green;

            _state = _ropuClient.State.ToString();
        }


        async Task UpdateIdleGroup()
        {
            if(_ropuClient.IdleGroup != null)
            {
                var idleGroup = await _groupsClient.Get(_ropuClient.IdleGroup.Value);
                if(idleGroup != null)
                {
                    IdleGroup = idleGroup.Name;
                    IdleGroupImage = idleGroup.Image;
                }
                return;
            }
            IdleGroup = "None";
            IdleGroupImage = null;
        }

        bool _initialized = false;

        public override async Task Initialize()
        {
            if(!_initialized)
            {
                _initialized = true;
                await _webClient.WaitForLogin();
                _settingsManager.ClientSettings.UserId = (await _usersClient.GetCurrentUser())?.Id;

                if(_settingsManager.ClientSettings.UserId == null) throw new InvalidOperationException("UserId is not set");

                var groups = (await _groupsClient.GetUsersGroups(_settingsManager.ClientSettings.UserId.Value));
                _ropuClient.IdleGroup = groups.Length == 0 ? null : (ushort?)groups[0];

                await UpdateIdleGroup();

                if(!await _permissionService.RequestAudioRecordPermission())
                {
                    //TODO: might need to go into a lissening only mode
                }

                await ChangeState();

                await _ropuClient.Run();
            }
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
                case StateId.NoGroup:
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

            var callGroup = InCall(state) && _ropuClient.CallGroup.HasValue ? await _groupsClient.Get(_ropuClient.CallGroup.Value) : null;
            CallGroup = callGroup?.Name == null ? "" : callGroup.Name;
            CallGroupImage = callGroup?.Image;

            await SetupCircleText(state);

            var user = state == StateId.InCallReceiving ? 
                (_ropuClient.Talker.HasValue ? await _usersClient.Get(_ropuClient.Talker.Value) : null) :
                null;
            Talker = user?.Name;
            if(user != null)
            {
                TalkerImage = await _imageClient.GetImage(user.ImageHash);
            }
        }

        async Task SetupCircleText(StateId state)
        {
            if(state == StateId.NoGroup)
            {
                CircleText = "None";
                return;
            }
            CircleText = (InCall(state) && _ropuClient.CallGroup.HasValue ? 
                (await _groupsClient.Get(_ropuClient.CallGroup.Value))?.Name : 
                _ropuClient.IdleGroup.HasValue ? (await _groupsClient.Get(_ropuClient.IdleGroup.Value))?.Name : null).EmptyIfNull();
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
            get => _ropuClient.IdleGroup == null ? "" : _ropuClient.IdleGroup.Value.ToString();
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

        public readonly ColorT Blue; 
        public readonly ColorT Green;
        public readonly ColorT Gray;
        public readonly ColorT Red;

        ColorT _receivingColor;
        public ColorT ReceivingColor
        {
            get => _receivingColor;
            set => SetProperty(ref _receivingColor, value);
        }

        ColorT _transmittingColor;
        public ColorT TransmittingColor
        {
            get => _transmittingColor;
            set => SetProperty(ref _transmittingColor, value);
        }

        

        ColorT _pttColor;
        public ColorT PttColor
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
            set
            {
                SetProperty(ref _idleGroup, value);
            }
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

        public ICommand SelectIdleGroup => new ActionCommand(() => 
        {
            _navigator.Navigate("SelectIdleGroupView");
        });
    }
}