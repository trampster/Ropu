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

namespace Ropu.ClientUI
{

    public class MainViewModel : BaseViewModel
    {
        RopuClient _ropuClient;

        public MainViewModel(RopuClient ropuClient)
        {
            _ropuClient = ropuClient;
            _ropuClient.StateChanged += (sender, args) => State = _ropuClient.State.ToString();
            State = _ropuClient.State.ToString();
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

        public ICommand PttDownCommand => new ActionCommand(() => PttState = "PTT Down");
        public ICommand PttUpCommand => new ActionCommand(() => PttState = "PTT Up");
    }

    public class MainForm : Form
    {
        public MainForm (MainViewModel mainViewModel)
        {
            Title = "Ropu Client";
            ClientSize = new Size(200, 200);

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

            Content = new TableLayout
            {
                Rows = 
                {
                    stateLabel,
                    pttStateLabel, 
                    button,
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
            var controllingFunctionClient = new ControllingFunctionClient(protocolSwitch);
            var mediaClient = new MediaClient(protocolSwitch);
            var callManagementProtocol = new LoadBalancerProtocol(new PortFinder(), 5079);

            var ipAddress = IPAddress.Parse(myAddress);

            IPEndPoint loadBalancerEndpoint = new IPEndPoint(IPAddress.Parse(loadBalancerIP), loadBalancerPort);
            var ropuClient = new RopuClient(protocolSwitch, controllingFunctionClient, ipAddress, callManagementProtocol, loadBalancerEndpoint, settings);
            var ropuClientTask = ropuClient.Run();

            new Application().Run(new MainForm(new MainViewModel(ropuClient)));
            ropuClientTask.Wait();
        }
    }
}
