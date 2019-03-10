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

namespace Ropu.ClientUI
{

    public class MainViewModel : BaseViewModel
    {
        readonly RopuClient _ropuClient;
        readonly IClientSettings _clientSettings;
        readonly IGroupsClient _groupsClient;
        readonly IUsersClient _usersClient;

        public MainViewModel(RopuClient ropuClient, IClientSettings clientSettings, IGroupsClient groupsClient, IUsersClient usersClient)
        {
            _ropuClient = ropuClient;
            _ropuClient.StateChanged += (sender, args) => 
            {
                Application.Instance.Invoke(ChangeState);
            };
            _groupsClient = groupsClient;
            _usersClient = usersClient;
            _clientSettings = clientSettings;

            _state = _ropuClient.State.ToString();
            _ropuClient.IdleGroup = 4242;
            _idleGroup = _groupsClient.Get(_ropuClient.IdleGroup).Name;
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

        void ChangeState()
        {
            var state = _ropuClient.State;
            State = state.ToString();
            switch (state)
            {
                case StateId.Start:
                case StateId.Unregistered:
                case StateId.Deregistering:
                    PttColor = Gray;
                    break;
                case StateId.Registered:
                case StateId.InCallIdle:
                case StateId.StartingCall:
                case StateId.InCallRequestingFloor:
                    PttColor = Blue;
                    break;
                case StateId.InCallReceiving:
                    PttColor = Red;
                    break;
                case StateId.InCallTransmitting:
                case StateId.InCallReleasingFloor:
                    PttColor = Green;
                    break;
                default:
                    throw new Exception("Unhandled Call State");
            }

            Transmitting = state == StateId.InCallTransmitting;

            CallGroup = InCall(state) ? _groupsClient.Get(_ropuClient.CallGroup).Name : null;

            CircleText = InCall(state) ? 
                _groupsClient.Get(_ropuClient.CallGroup).Name : 
                _groupsClient.Get(_ropuClient.IdleGroup).Name;

            Talker = state == StateId.InCallReceiving ? _usersClient.Get(_ropuClient.Talker.Value).Name : null;
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

        string _idleGroup;
        public string IdleGroup
        {
            get => _idleGroup;
            set => SetProperty(ref _idleGroup, value);
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
        public MainForm (MainViewModel mainViewModel)
        {
            Title = "Ropu Client";
            ClientSize = new Size(300, 500);

            _pttCircle = new PttPage();
            _pttCircle.BindDataContext(c => c.ButtonDownCommand, (MainViewModel model) => model.PttDownCommand);
            _pttCircle.BindDataContext(c => c.ButtonUpCommand, (MainViewModel model) => model.PttUpCommand);
            _pttCircle.PttColorBinding.BindDataContext<MainViewModel>(m => m.PttColor);

            _pttCircle.TalkerBinding.BindDataContext<MainViewModel>(m => m.Talker);
            _pttCircle.IdleGroupBinding.BindDataContext<MainViewModel>(m => m.IdleGroup);
            _pttCircle.CallGroupBinding.BindDataContext<MainViewModel>(m => m.CallGroup);
            _pttCircle.CircleTextBinding.BindDataContext<MainViewModel>(m => m.CircleText);
            _pttCircle.TransmittingBinding.BindDataContext<MainViewModel>(m => m.Transmitting);
            _pttCircle.TransmittingAnimationColor = MainViewModel.Green;
            _pttCircle.ReceivingAnimationColor = MainViewModel.Red;

            Content = _pttCircle;
            DataContext = mainViewModel;
        }


        [STAThread]
        static void Main(string[] args)
        {
            const ushort controlPortStarting = 5061;
            const string myAddress = "192.168.1.6";
            const string loadBalancerIP =  "192.168.1.6";
            const int loadBalancerPort = 5069;

            var settings = new ClientSettings();
            if(args.Length > 0 && uint.TryParse(args[0], out uint userId))
            {
                settings.UserId = userId;
            }

            var protocolSwitch = new ProtocolSwitch(controlPortStarting, new PortFinder());
            var servingNodeClient = new ServingNodeClient(protocolSwitch);
            var audioSource = new FileAudioSource("/home/daniel/Music/oliver-twist-001.wav");
            //var audioSource = new AlsaAudioSource();
            var audioPlayer = new AlsaAudioPlayer();
            var audioCodec = new RawCodec();
            var mediaClient = new MediaClient(protocolSwitch, audioSource, audioPlayer, audioCodec, settings);
            var callManagementProtocol = new LoadBalancerProtocol(new PortFinder(), 5079);

            var ipAddress = IPAddress.Parse(myAddress);

            IPEndPoint loadBalancerEndpoint = new IPEndPoint(IPAddress.Parse(loadBalancerIP), loadBalancerPort);
            var ropuClient = new RopuClient(protocolSwitch, servingNodeClient, mediaClient, ipAddress, callManagementProtocol, loadBalancerEndpoint, settings);

            var application = new RopuApplication(ropuClient);

            var groupsClient = new HardcodedGroupsClient();
            var usersClient = new HardcodedUsersClient();

            var mainForm = new MainForm(new MainViewModel(ropuClient, settings, groupsClient, usersClient));
            mainForm.Icon = new Icon("../Icon/Ropu.ico");
            application.Run(mainForm);
        }
    }
}
