using System;
using Eto.Forms;
using Eto.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Ropu.Client;
using System.Net;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;
using Ropu.Client.Alsa;
using Ropu.Client.FileAudio;
using Ropu.Shared.Groups;
using Ropu.Client.Opus;
using System.Linq;
using Ropu.Client.JitterBuffer;
using Ropu.ClientUI.Services;
using Ropu.Client.PulseAudio;
using Ropu.Shared.Web;
using System.Threading.Tasks;

namespace Ropu.ClientUI
{

    public class MainViewModel : BaseViewModel
    {
        readonly RopuClient _ropuClient;
        readonly IClientSettings _clientSettings;
        readonly IGroupsClient _groupsClient;
        readonly IUsersClient _usersClient;
        readonly ImageClient _imageClient;

        public MainViewModel(RopuClient ropuClient, IClientSettings clientSettings, IGroupsClient groupsClient, IUsersClient usersClient, ImageClient imageClient)
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

        public async Task Initialize()
        {
            _ropuClient.IdleGroup = (await _groupsClient.GetUsersGroups(_clientSettings.UserId))[0];

            var idleGroup = await _groupsClient.Get(_ropuClient.IdleGroup);
            _idleGroup = idleGroup.Name;
            _idleGroupImage = idleGroup.Image;
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
            CallGroup = callGroup?.Name;
            CallGroupImage = callGroup?.Image;

            CircleText = InCall(state) ? 
                (await _groupsClient.Get(_ropuClient.CallGroup)).Name : 
                (await _groupsClient.Get(_ropuClient.IdleGroup)).Name;

            var user = state == StateId.InCallReceiving ? await _usersClient.Get(_ropuClient.Talker.Value) : null;
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

        string _talker;
        public string Talker
        {
            get => _talker;
            set => SetProperty(ref _talker, value);
        }

        byte[] _talkerImage;
        public byte[] TalkerImage
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

        string _callGroup;
        public string CallGroup
        {
            get => _callGroup;
            set => SetProperty(ref _callGroup, value);
        }

        byte[] _callGroupImage;
        public byte[] CallGroupImage
        {
            get => _callGroupImage;
            set => SetProperty(ref _callGroupImage, value);
        }

        string _idleGroup;
        public string IdleGroup
        {
            get => _idleGroup;
            set => SetProperty(ref _idleGroup, value);
        }

        byte[] _idleGroupImage;
        public byte[] IdleGroupImage
        {
            get => _idleGroupImage;
            set => SetProperty(ref _idleGroupImage, value);
        }

        string _circleText;
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

        public ICommand UserIdCommand => new ActionCommand(() => 
        {
            bool valid = uint.TryParse(UserId, out uint userId);
            UserIdError =  valid ? "" : "Invalid";
            if(!valid) return;
            UserIdError = "";
            _clientSettings.UserId = userId;
        });
    }

    public class MainForm : Form
    {
        readonly PttPage _pttCircle;
        public MainForm (MainViewModel mainViewModel, PttPage pttPage)
        {
            Title = "Ropu Client";
            ClientSize = new Size(300, 500);

            _pttCircle = pttPage;
            _pttCircle.BindDataContext(c => c.ButtonDownCommand, (MainViewModel model) => model.PttDownCommand);
            _pttCircle.BindDataContext(c => c.ButtonUpCommand, (MainViewModel model) => model.PttUpCommand);
            _pttCircle.PttColorBinding.BindDataContext<MainViewModel>(m => m.PttColor);

            _pttCircle.TalkerBinding.BindDataContext<MainViewModel>(m => m.Talker);
            _pttCircle.TalkerImageBinding.BindDataContext<MainViewModel>(m => m.TalkerImage);
            _pttCircle.IdleGroupBinding.BindDataContext<MainViewModel>(m => m.IdleGroup);
            _pttCircle.IdleGroupImageBinding.BindDataContext<MainViewModel>(m => m.IdleGroupImage);
            _pttCircle.CallGroupBinding.BindDataContext<MainViewModel>(m => m.CallGroup);
            _pttCircle.CallGroupImageBinding.BindDataContext<MainViewModel>(m => m.CallGroupImage);
            _pttCircle.CircleTextBinding.BindDataContext<MainViewModel>(m => m.CircleText);
            _pttCircle.TransmittingBinding.BindDataContext<MainViewModel>(m => m.Transmitting);
            _pttCircle.TransmittingAnimationColor = MainViewModel.Green;
            _pttCircle.ReceivingAnimationColor = MainViewModel.Red;

            Content = _pttCircle;
            DataContext = mainViewModel;

            this.Shown += async (sender, args) => 
            {
                await mainViewModel.Initialize();
            };
        }


        static void Main(string[] args)
        {
            const ushort controlPortStarting = 5061;
            const int loadBalancerPort = 5069;

            var settings = new CommandLineClientSettings();

            if(!settings.ParseArgs(args))
            {
                return;
            }

            var protocolSwitch = new ProtocolSwitch(controlPortStarting, new PortFinder());
            var servingNodeClient = new ServingNodeClient(protocolSwitch);

            IAudioSource audioSource = 
                settings.FileMediaSource != null ?
                (IAudioSource) new FileAudioSource(settings.FileMediaSource) :
                (IAudioSource) new PulseAudioSimple(StreamDirection.Record, "RopuInput");

            var audioPlayer = new PulseAudioSimple(StreamDirection.Playback, "RopuOutput");
            var audioCodec = new OpusCodec();
            var jitterBuffer = new AdaptiveJitterBuffer(2, 50);
            var mediaClient = new MediaClient(protocolSwitch, audioSource, audioPlayer, audioCodec, jitterBuffer, settings);
            var callManagementProtocol = new LoadBalancerProtocol(new PortFinder(), 5079);

            IPEndPoint loadBalancerEndpoint = new IPEndPoint(settings.LoadBalancerIPAddress, loadBalancerPort);
            var beepPlayer = new BeepPlayer(new PulseAudioSimple(StreamDirection.Record, "RopuBeeps"));
            var ropuClient = new RopuClient(protocolSwitch, servingNodeClient, mediaClient, callManagementProtocol, loadBalancerEndpoint, settings, beepPlayer);

            var application = new RopuApplication(ropuClient);

            var imageService = new ImageService();
            var credentialsProvider = new CredentialsProvider()
            {
                Email = settings.Email,
                Password = settings.Password
            };
            //TODO: get web address from config
            var webClient = new RopuWebClient("https://localhost:5001/", credentialsProvider);
            var groupsClient = new GroupsClient(webClient);
            var usersClient = new UsersClient(webClient);
            settings.UserId = usersClient.GetCurrentUser().Result.Id;
            var imageClient = new ImageClient(webClient);
            var pttPage = new PttPage(imageService);


            var mainForm = new MainForm(new MainViewModel(ropuClient, settings, groupsClient, usersClient, imageClient), pttPage);
            mainForm.Icon = imageService.Ropu;
            application.Run(mainForm);
        }
    }
}
