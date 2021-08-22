using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ropu.Client.StateModel;
using Ropu.Shared;
using Ropu.Shared.LoadBalancing;
using Ropu.Shared.ControlProtocol;
using Ropu.Shared.Web;
using System.Globalization;
using System.Linq;

namespace Ropu.Client
{
    public class RopuClient : IControllingFunctionPacketHandler
    {
        const ushort _port = 1000;

        readonly ServingNodeClient _servingNodeClient;
        readonly ProtocolSwitch _protocolSwitch;
        readonly IMediaClient _mediaClient;

        readonly IClientSettings _clientSettings;

        RopuState _start;
        RopuState _registered;
        RopuState _unregistered;
        RopuState _noGroup;
        RopuState _deregistering;
        readonly RopuState _startingCall;
        RopuState _inCallIdle;
        RopuState _inCallReceiveing;
        RopuState _inCallTransmitting;
        readonly RopuState _inCallReleasingFloor;
        readonly RopuState _inCallRequestingFloor;

        StateManager<StateId, EventId> _stateManager;
        LoadBalancerProtocol _loadBalancerProtocol;

        readonly Ropu.Shared.Timer _retryTimer;
        IPEndPoint? _loadBalancerEndPoint;
        uint _registeredUserId = 0;

        public event EventHandler<EventArgs>? StateChanged;
        /// <summary>
        /// this is the group the user has selected, it is the group to be called when they PTT and 
        /// the group to return to after the call
        /// </summary>
        ushort? _idleGroup;
        /// <summary>
        /// The group of the current call, or the call we are trying to start
        /// </summary>
        ushort? _callGroup;

        Task? _heartbeatTask;
        CancellationTokenSource _heartbeatCancellationTokenSource = new CancellationTokenSource();
        ManualResetEvent _heartbeatOnEvent = new ManualResetEvent(false);
        readonly IBeepPlayer _beepPlayer;
        readonly RopuWebClient _webClient;
        readonly KeysClient _keysClient;

        public RopuClient(
            ProtocolSwitch protocolSwitch, 
            ServingNodeClient servingNodeClient, 
            IMediaClient mediaClient,
            LoadBalancerProtocol loadBalancerProtocol,
            IClientSettings clientSettings,
            IBeepPlayer beepPlayer,
            RopuWebClient webClient,
            KeysClient keysClient)
        {
            _webClient = webClient;
            _beepPlayer = beepPlayer;
            _clientSettings = clientSettings;
            _loadBalancerProtocol = loadBalancerProtocol;
            _protocolSwitch = protocolSwitch;
            _servingNodeClient = servingNodeClient;
            _mediaClient = mediaClient;
            _servingNodeClient.SetControllingFunctionHandler(this);
            _retryTimer = new Ropu.Shared.Timer();
            _keysClient = keysClient;

            var allEvents = (EventId[])Enum.GetValues(typeof(EventId));

            //start
            _start = new RopuState(StateId.Start);
            _stateManager = new StateManager<StateId, EventId>(_start);
            _stateManager.AddState(_start);
            _start.AddTransitions(allEvents, () => _start);

            //registered
            _registered = new RopuState(StateId.Registered)
            {
                Entry = async token => 
                { 
                    if(_clientSettings.UserId == null) throw new InvalidOperationException("UserId is not set");
                    _callGroup = IdleGroup;
                    _registeredUserId = _clientSettings.UserId.Value;
                    if(_heartbeatTask == null)
                    {
                        _heartbeatTask = Heartbeat(_heartbeatCancellationTokenSource.Token);
                    }
                    _heartbeatOnEvent.Set(); //allows the heartbeat to continue
                    await Task.Run(() => {});
                },
            };
            _stateManager.AddState(_registered);
            _registered.AddTransition(EventId.CallRequest, () => _startingCall!);
            _registered.AddTransition(EventId.PttDown, () => _startingCall!);
            _registered.AddTransition(EventId.RegistrationResponseReceived, () => _registered);
            _registered.AddTransition(EventId.CallStartFailed, () => _registered);
            _registered.AddTransition(EventId.PttUp, () => _registered);
            _registered.AddTransition(EventId.GroupSelected, () => _registered);

            //unregistered
            _noGroup = new RopuState(StateId.NoGroup);
            _unregistered = new RopuState(StateId.Unregistered)
            {
                Entry = async token => 
                {
                    _heartbeatOnEvent.Reset(); //stops the heartbeat
                    await Register(token);
                }
            };
            _unregistered.AddTransition(EventId.RegistrationResponseReceived, () => IdleGroup == null ? _noGroup : _registered);
            _unregistered.AddTransition(EventId.FloorIdle, () => _unregistered);
            _unregistered.AddTransition(EventId.FloorTaken, () => _unregistered);
            _unregistered.AddTransition(EventId.CallEnded, () => _unregistered);
            _unregistered.AddTransition(EventId.CallRequest, () => _unregistered);
            _unregistered.AddTransition(EventId.CallStartFailed, () => _unregistered);
            _unregistered.AddTransition(EventId.HeartbeatFailed, () => _unregistered);
            _unregistered.AddTransition(EventId.PttUp, () => _unregistered);
            _unregistered.AddTransition(EventId.PttDown, () => _unregistered);
            _unregistered.AddTransition(EventId.GroupSelected, () => _unregistered);

            _stateManager.AddState(_unregistered);

            //no group
            _noGroup.AddTransition(EventId.PttUp, () => _noGroup);
            _noGroup.AddTransition(EventId.PttDown, () => _noGroup);
            _noGroup.AddTransition(EventId.GroupSelected, () => _registered);
            _noGroup.AddTransition(EventId.RegistrationResponseReceived, () => _noGroup);
            _noGroup.AddTransition(EventId.CallRequest, () => _noGroup);
            _noGroup.AddTransition(EventId.CallStartFailed, () => _noGroup);


            _stateManager.AddState(_noGroup);

            //deregistering
            _deregistering = new RopuState(StateId.Deregistering)
            {
                Entry = async token => await Deregister(token),
            };
            _deregistering.AddTransition(EventId.DeregistrationResponseReceived, () => _unregistered);
            _deregistering.AddTransitions(allEvents.Where(e => e != EventId.DeregistrationResponseReceived), () => _deregistering);
            _deregistering.AddTransition(EventId.GroupSelected, () => _deregistering);
            _stateManager.AddState(_deregistering);

            //starting call
            _startingCall = new RopuState(StateId.StartingCall)
            {
                Entry = async token => await StartCall(token),
                Exit = newState =>
                {
                    if(newState != _inCallTransmitting && newState != _inCallRequestingFloor)
                    {
                        _mediaClient.StopSendingAudio();
                    }
                }
            };
            _startingCall.AddTransition(EventId.CallStartFailed, () => _registered);
            _startingCall.AddTransition(EventId.PttUp, () => _startingCall);
            _startingCall.AddTransition(EventId.PttDown, () => _startingCall);
            _startingCall.AddTransition(EventId.RegistrationResponseReceived, () => _startingCall);
            _startingCall.AddTransition(EventId.CallRequest, () => _startingCall);
            _startingCall.AddTransition(EventId.GroupSelected, () => _startingCall);

            _stateManager.AddState(_startingCall);

            //in call idle
            _inCallIdle = new RopuState(StateId.InCallIdle);
            _inCallIdle.AddTransition(EventId.PttDown, () => _inCallRequestingFloor!);
            _inCallIdle.AddTransition(EventId.PttUp, () => _inCallIdle);
            _inCallIdle.AddTransition(EventId.RegistrationResponseReceived, () => _inCallIdle);
            _inCallIdle.AddTransition(EventId.CallRequest, () => _inCallIdle);
            _inCallIdle.AddTransition(EventId.CallStartFailed, () => _registered);
            _inCallIdle.AddTransition(EventId.GroupSelected, () => _inCallIdle);
            _stateManager.AddState(_inCallIdle);

            //in call receiving
            _inCallReceiveing = new RopuState(StateId.InCallReceiving);
            _inCallReceiveing.AddTransition(EventId.PttDown, () => _inCallReceiveing);
            _inCallReceiveing.AddTransition(EventId.PttUp, () => _inCallReceiveing);
            _inCallReceiveing.AddTransition(EventId.RegistrationResponseReceived, () => _inCallReceiveing);
            _inCallReceiveing.AddTransition(EventId.CallRequest, () => _inCallReceiveing);
            _inCallReceiveing.AddTransition(EventId.CallStartFailed, () => _registered);
            _inCallReceiveing.AddTransition(EventId.GroupSelected, () => _inCallReceiveing);
            _stateManager.AddState(_inCallReceiveing);

            //in call transmitting
            _inCallTransmitting = new RopuState(StateId.InCallTransmitting)
            {
                Entry = async token => 
                {
                    _beepPlayer.PlayGoAhead();
                    await Task.Run(() => {});
                },
                Exit = newState =>
                {
                    if(newState != _inCallTransmitting && newState != _inCallRequestingFloor)
                    {
                        _mediaClient.StopSendingAudio();
                    }
                }
            };
            _inCallTransmitting.AddTransition(EventId.PttUp, () => _inCallReleasingFloor!);
            _inCallTransmitting.AddTransition(EventId.RegistrationResponseReceived, () => _inCallTransmitting);
            _inCallTransmitting.AddTransition(EventId.CallRequest, () => _inCallTransmitting);
            _inCallTransmitting.AddTransition(EventId.CallStartFailed, () => _inCallTransmitting);
            _inCallTransmitting.AddTransition(EventId.PttDown, () => _inCallTransmitting);
            _inCallTransmitting.AddTransition(EventId.GroupSelected, () => _inCallTransmitting);

            _stateManager.AddState(_inCallTransmitting);

            //in call requesting floor
            _inCallRequestingFloor = new RopuState(StateId.InCallRequestingFloor)
            {
                Entry = token => 
                {
                    if(_callGroup == null) return Task.CompletedTask;
                    if(_clientSettings.UserId == null) throw new InvalidOperationException("UserId is not set");
                    _servingNodeClient.SendFloorRequest(_callGroup.Value, _clientSettings.UserId.Value);
                    StartSendingAudio();
                    return Task.CompletedTask;
                },
                Exit = newState =>
                {
                    if(newState != _inCallTransmitting && newState != _inCallRequestingFloor)
                    {
                        _mediaClient.StopSendingAudio();
                        _beepPlayer.PlayDenied();
                    }
                }
            };
            _inCallRequestingFloor.AddTransition(EventId.RegistrationResponseReceived, () => _inCallReleasingFloor!);
            _inCallRequestingFloor.AddTransition(EventId.CallRequest, () => _inCallReleasingFloor!);
            _inCallRequestingFloor.AddTransition(EventId.CallStartFailed, () => _registered);
            _inCallRequestingFloor.AddTransition(EventId.PttDown, () => _inCallRequestingFloor);
            _inCallRequestingFloor.AddTransition(EventId.PttUp, () => _inCallIdle);
            _inCallRequestingFloor.AddTransition(EventId.GroupSelected, () => _inCallRequestingFloor);
            _stateManager.AddState(_inCallRequestingFloor);

            //in call releasing floor
            _inCallReleasingFloor = new RopuState(StateId.InCallReleasingFloor)
            {
                Entry = async token => 
                {
                    if(_callGroup == null) throw new InvalidOperationException("No call group");
                    if(_clientSettings.UserId == null) throw new InvalidOperationException("UserId is not set");
                    while(!token.IsCancellationRequested)
                    {
                        _servingNodeClient.SendFloorReleased(_callGroup.Value, _clientSettings.UserId.Value);
                        await Task.Run(() => token.WaitHandle.WaitOne(1000));
                    }
                }
            };
            _inCallReleasingFloor.AddTransition(EventId.FloorGranted, () => _inCallReleasingFloor);
            _inCallReleasingFloor.AddTransition(EventId.RegistrationResponseReceived, () => _inCallReleasingFloor);
            _inCallReleasingFloor.AddTransition(EventId.CallRequest, () => _inCallReleasingFloor);
            _inCallReleasingFloor.AddTransition(EventId.CallStartFailed, () => _registered);
            _inCallReleasingFloor.AddTransition(EventId.PttDown, () => _inCallRequestingFloor);
            _inCallReleasingFloor.AddTransition(EventId.PttUp, () => _inCallReleasingFloor);
            _inCallReleasingFloor.AddTransition(EventId.GroupSelected, () => _inCallReleasingFloor);

            _stateManager.AddState(_inCallReleasingFloor);


            _inCallReleasingFloor.AddTransition(EventId.FloorGranted, () => _inCallReleasingFloor);


            _stateManager.StateChanged += (sender, args) => 
            {
                Console.WriteLine($"State Changed {this._stateManager.CurrentState}");
                StateChanged?.Invoke(this, args);
            };
            
            _stateManager.AddTransitionToAll(EventId.HeartbeatFailed, () => _unregistered, stateId => true);
            _stateManager.AddTransitionToAll(EventId.NotRegistered, () => _unregistered, stateId => true);
            _stateManager.AddTransitionToAll(EventId.CallEnded, () => _registered, stateId => stateId != StateId.Unregistered && stateId != StateId.Start && stateId != StateId.Deregistering);
            _stateManager.AddTransitionToAll(EventId.FloorIdle, () => _inCallIdle, IsRegistered);
            _stateManager.AddTransitionToAll(EventId.FloorTaken, () => _inCallReceiveing, IsRegistered);
            _stateManager.AddTransitionToAll(EventId.FloorGranted, () => _inCallTransmitting, stateId => stateId != StateId.InCallReleasingFloor);
            _stateManager.AddTransitionToAll(EventId.DeregistrationResponseReceived, () => _unregistered, stateId => true);
            _stateManager.AddTransitionToAll(EventId.GroupDeselected, () => _noGroup, stateId => true);

            _stateManager.CheckEventsAreHandledByAll((EventId[])Enum.GetValues(typeof(EventId)));
        }

        async void StartSendingAudio()
        {
            if(_callGroup == null) throw new InvalidOperationException("Call group is not set");
            await _mediaClient.StartSendingAudio(_callGroup.Value);
        }

        bool IsRegistered(StateId stateId)
        {
            return  
                stateId != StateId.Unregistered && 
                stateId != StateId.Deregistering &&
                stateId != StateId.Start;
        }

        public StateId State
        {
            get => _stateManager.CurrentState.Identifier;
        }

        System.Timers.Timer CreateTimer(int interval, Action callback)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = interval;
            timer.AutoReset = false;
            timer.Elapsed += (sender, args) => callback();
            return timer;
        }

        public async Task Run()
        {
            await GetLoadBalancerIPEndpoint();

            var cancellationTokenSource = new CancellationTokenSource();
            var keysClient = _keysClient.Run(cancellationTokenSource.Token);
            var protocolSwitchTask = _protocolSwitch.Run();

            _loadBalancerProtocol.UserId = _clientSettings.UserId;
            var loadBalancerTask = _loadBalancerProtocol.Run();
            var playAudioTask = _mediaClient.PlayAudio();
            _stateManager.SetState(_unregistered, _start);
            await TaskCordinator.WaitAll(protocolSwitchTask, loadBalancerTask, playAudioTask, keysClient);
        }

        public async Task GetLoadBalancerIPEndpoint()
        {
            while(true)
            {
                var response = await _webClient.Get<string>("api/Services/LoadBalancerIPEndpoint");
                if(response.StatusCode != HttpStatusCode.OK)
                {
                    Console.Error.WriteLine($"Failed to get LoadBalancer IP Endpoint with status {response.StatusCode}");
                    await Task.Delay(5000);
                    continue;
                }

                string endPoint = await response.GetString();
                if(endPoint == null || endPoint == string.Empty)
                {
                    Console.Error.WriteLine($"Failed to get LoadBalancer IP Endpoint.");
                    await Task.Delay(5000);
                    continue;
                }

                _loadBalancerEndPoint = ParseIPEndPoint(endPoint);
                return;
            }
        }

        //remove once we upgrade dotnet core which has IPEndPoint.Parse
        public static IPEndPoint ParseIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if(ep.Length != 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if(!IPAddress.TryParse(ep[0], out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
            int port;
            if(!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }

        readonly ManualResetEvent _heartbeatResetEvent = new ManualResetEvent(false);

        async ValueTask<bool> WaitForEvent(CancellationToken token, ManualResetEvent resetEvent, int milliseconds)
        {
            return await Task.Run(() => 
            {
                var waitResult = WaitHandle.WaitAny(new []{token.WaitHandle, resetEvent}, milliseconds);
                return waitResult == 1;
            });
        }

        async Task WaitForEvent(ManualResetEvent resetEvent)
        {
            await Task.Run(() => 
            {
                resetEvent.WaitOne();
            });
        }

        async ValueTask<bool> WaitForCancel(CancellationToken token, int milliseconds)
        {
            return await Task.Run(() => 
            {
                return token.WaitHandle.WaitOne(milliseconds);
            });
        }


        async Task Heartbeat(CancellationToken token)
        {
            if(_clientSettings.UserId == null) throw new InvalidOperationException("UserId is not set");

            while(!token.IsCancellationRequested)
            {
                await WaitForEvent(_heartbeatOnEvent);
                await Task.Delay(25000, token);
                _heartbeatResetEvent.Reset();
                bool heartbeatReceived = false;
                for(int attemptNumber = 0; attemptNumber < 3; attemptNumber++)
                {
                    _servingNodeClient.SendHeartbeat(_clientSettings.UserId.Value);
                    heartbeatReceived = await WaitForEvent(token, _heartbeatResetEvent, 1000);
                    if(token.IsCancellationRequested) return;
                    if(heartbeatReceived)
                    {
                        break;
                    }
                }
                if(heartbeatReceived == false)
                {
                    Console.WriteLine("Heartbeat failed");
                    _stateManager.HandleEventNonBlocking(EventId.HeartbeatFailed);
                    return;
                }
            }
        }

        async Task Register(CancellationToken token)
        {
            if(_clientSettings.UserId == null) throw new InvalidOperationException("UserId is not set yet");
            while(!token.IsCancellationRequested)
            {
                if(_clientSettings.UserId == 0)
                {
                    if(await WaitForCancel(token, 2000))
                    {
                        return;
                    }
                    continue;
                }
                if(_protocolSwitch.ServingNodeEndpoint == null)
                {
                    if(_loadBalancerEndPoint == null) throw new InvalidOperationException("Don't know the load balancer endpoint");
                    var servingNodeEndpoint = await _loadBalancerProtocol.RequestServingNode(_loadBalancerEndPoint);
                    if(servingNodeEndpoint == null)
                    {
                        Console.WriteLine("Failed to get a serving node");
                        if(await WaitForCancel(token, 2000)) 
                        {
                            return;
                        }
                        continue;
                    }
                    _protocolSwitch.ServingNodeEndpoint = servingNodeEndpoint;
                }

                Console.WriteLine($"Got serving node at {_protocolSwitch.ServingNodeEndpoint}");

                _servingNodeClient.Register(_clientSettings.UserId.Value);
                if(await WaitForCancel(token, 2000))
                {
                    return;
                }
            }
        }

        async Task Deregister(CancellationToken token)
        {
            if(_registeredUserId == 0)
            {
                _stateManager.SetState(_unregistered, _deregistering);
                return;
            }
            
            while(!token.IsCancellationRequested)
            {
                _servingNodeClient.Deregister(_registeredUserId);
                await WaitForCancel(token, 2000);
            }
        }

        public void StartCall(ushort groupId)
        {
            _callGroup = groupId;
            _stateManager.HandleEvent(EventId.CallRequest);
        }

        public ushort? CallGroup => _callGroup;

        async Task StartCall(CancellationToken token)
        {
            if(IdleGroup == null)
            {
                throw new InvalidOperationException("Cannot start call because we have no group");
            }
            if(_clientSettings.UserId == null) throw new InvalidOperationException("UserId is not set");
            _callGroup = IdleGroup;
            StartSendingAudio();//var ignore = _mediaClient.StartSendingAudio(_callGroup);

            while(!token.IsCancellationRequested)
            {
                Console.WriteLine($"sending StartGroupCall for group {_callGroup} {_protocolSwitch.ServingNodeEndpoint}");
                _servingNodeClient.StartGroupCall(_clientSettings.UserId.Value, _callGroup.Value);
                await WaitForCancel(token, 1000);
            }
        }

        public event EventHandler? IdleGroupChanged;

        public ushort? IdleGroup
        {
            get => _idleGroup;
            set
            {
                if(_idleGroup == value)
                {
                    return;
                }
                _idleGroup = value;
                if(value != null)
                {
                    _stateManager.HandleEvent(EventId.GroupSelected);
                }
                else
                {
                    _stateManager.HandleEvent(EventId.GroupDeselected);
                }
                IdleGroupChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        bool IsPttDown
        {
            get;
            set;
        }

        public void PttUp()
        {
            IsPttDown = false;
            _stateManager.HandleEvent(EventId.PttUp);
        }

        public void PttDown()
        {
            IsPttDown = true;
            _stateManager.HandleEvent(EventId.PttDown);
            if(_stateManager.CurrentState.Identifier == StateId.InCallReceiving)
            {
                _beepPlayer.PlayDenied();
            }
        }

        void StartRetryTimer(int duration, Action callback)
        {
            _retryTimer.Duration = 1000;
            _retryTimer.Callback = callback;
            _retryTimer.Start();
        }

        public void HandleRegistrationResponseReceived(Codec codec, ushort bitrate)
        {
            Console.WriteLine("Register Response Received");
            _stateManager.HandleEvent(EventId.RegistrationResponseReceived);
        }

        public void HandleCallStartFailed(CallFailedReason reason)
        {
            Console.WriteLine($"CallStartFailed with reason {reason}");
            _stateManager.HandleEvent(EventId.CallStartFailed);
        }

        public void HandleHeartbeatResponseReceived()
        {
            _heartbeatResetEvent.Set();
        }

        public void HandleNotRegisteredReceived()
        {
            _stateManager.HandleEvent(EventId.NotRegistered);
        }

        public void HandleRegisterResponse()
        {
            _stateManager.HandleEvent(EventId.DeregistrationResponseReceived);
        }

        public void HandleCallEnded(ushort groupId)
        {
            _stateManager.HandleEvent(EventId.CallEnded);
        }

        uint? _talker = null;
        public uint? Talker
        {
            get => _talker;
            set
            {
                _talker = value;
                _mediaClient.Talker = value;
            }
        }

        bool InCall()
        {
            var state = _stateManager.CurrentState.Identifier;
            switch(state)
            {
                case StateId.InCallIdle:
                case StateId.InCallReceiving:
                case StateId.InCallReleasingFloor:
                case StateId.InCallRequestingFloor:
                case StateId.InCallTransmitting:
                    return true;
                default:
                    return false;
            }
        }

        public void HandleFloorTaken(ushort groupId, uint userId)
        {
            Console.WriteLine($"FloorTaken from group {groupId} for user {userId}");

            if(InCall() && _callGroup != groupId)
            {
                Console.WriteLine("HandleFloorTaken Not Current call");
                return; //not for the current call;
            }
            _callGroup = groupId;
            Talker = userId;
            
            if(_clientSettings.UserId == userId)
            {
                _stateManager.HandleEvent(EventId.FloorGranted);
                return;
            }
            _stateManager.HandleEvent(EventId.FloorTaken);
        }

        public void HandleFloorIdle(ushort groupId)
        {
            Console.WriteLine($"FloorIdle from group {groupId}");
            if(InCall() && _callGroup != groupId)
            {
                Console.WriteLine("HandleFloorIdle but Not Current call");
                return; //not for the current call;
            }
            _callGroup = groupId;
            Talker = null;
            _stateManager.HandleEvent(EventId.FloorIdle);
        }
    }
}