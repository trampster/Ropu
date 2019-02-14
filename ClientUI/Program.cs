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

namespace Ropu.ClientUI
{

    public class MainViewModel : BaseViewModel
    {
        readonly RopuClient _ropuClient;
        readonly IClientSettings _clientSettings;

        public MainViewModel(RopuClient ropuClient, IClientSettings clientSettings)
        {
            _clientSettings = clientSettings;
            _ropuClient = ropuClient;
            _ropuClient.StateChanged += (sender, args) => 
            {
                Application.Instance.Invoke(() => State = _ropuClient.State.ToString());
            };
            _state = _ropuClient.State.ToString();
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
        public MainForm (MainViewModel mainViewModel)
        {
            Title = "Ropu Client";
            ClientSize = new Size(400, 200);

            var pttStateLabel = new Label();
            pttStateLabel.TextBinding.BindDataContext<MainViewModel>(m => m.PttState);

            var stateLabel = new Label();
            stateLabel.TextBinding.BindDataContext<MainViewModel>(m => m.State);

            var button = new PttButton()
            {
                Text = "Push To Talk"
            };
            button.BindDataContext(c => c.ButtonDownCommand, (MainViewModel model) => model.PttDownCommand);
            button.BindDataContext(c => c.ButtonUpCommand, (MainViewModel model) => model.PttUpCommand);


            var textBox = new TextBox();
            textBox.TextBinding.BindDataContext<MainViewModel>(m => m.UserId);
            var userIdErrorLabel = new Label();
            userIdErrorLabel.TextBinding.BindDataContext<MainViewModel>(m => m.UserIdError);
            var userButton = new Button(){Text="Set"};
            userButton.BindDataContext(c => c.Command, (MainViewModel model) => model.UserIdCommand);


            var grouptextBox = new TextBox();
            grouptextBox.TextBinding.BindDataContext<MainViewModel>(m => m.GroupId);
            var groupErrorLabel = new Label();
            groupErrorLabel.TextBinding.BindDataContext<MainViewModel>(m => m.GroupIdError);

            Content = new TableLayout
            {
                Padding = new Padding(10,10,10,10),
                Spacing = new Size(5,5),
                Rows = 
                {
                    new TableLayout(){ Rows = { new TableRow(new TableCell(new Label { Text = "State: "}), stateLabel)}},
                    new TableLayout(){ Rows = { new TableRow(new TableCell(
                        new Label { Text = "User ID: ", VerticalAlignment = VerticalAlignment.Center}), 
                        textBox, 
                        userButton,
                        userIdErrorLabel
                        )}},
                    new TableLayout(){ Rows = { new TableRow(new TableCell(new Label { Text = "Group ID: ", VerticalAlignment = VerticalAlignment.Center}), grouptextBox, groupErrorLabel)}},
                    pttStateLabel, 
                    new TableRow(button),
                }
            };
            DataContext = mainViewModel;
        }
        

        [STAThread]
        static void Main()
        {

            const ushort controlPortStarting = 5061;
            const string myAddress = "192.168.1.6";
            const string loadBalancerIP =  "192.168.1.6";
            const int loadBalancerPort = 5069;

            var settings = new ClientSettings();

            var protocolSwitch = new ProtocolSwitch(controlPortStarting, new PortFinder());
            var servingNodeClient = new ServingNodeClient(protocolSwitch);
            var audioSource = new AlsaAudioSource();
            var audioCodec = new RawCodec();
            var mediaClient = new MediaClient(protocolSwitch, audioSource, audioCodec, settings);
            var callManagementProtocol = new LoadBalancerProtocol(new PortFinder(), 5079);

            var ipAddress = IPAddress.Parse(myAddress);

            IPEndPoint loadBalancerEndpoint = new IPEndPoint(IPAddress.Parse(loadBalancerIP), loadBalancerPort);
            var ropuClient = new RopuClient(protocolSwitch, servingNodeClient, mediaClient, ipAddress, callManagementProtocol, loadBalancerEndpoint, settings);

            var application = new RopuApplication(ropuClient);
            application.Run(new MainForm(new MainViewModel(ropuClient, settings)));
        }
    }
}
