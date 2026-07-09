using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ropu.Logging;
using Ropu.Protocol;
using Ropu.Shared;

namespace Ropu.Client;

public class RopuClient
{
    readonly Guid _clientId;
    readonly BalancerClient _balancerClient;
    readonly RouterClient _routerClient;
    readonly ILogger _logger;

    readonly ConcurrentDictionary<Guid, SocketAddress> _addressLookup = new();

    public RopuClient(
        Guid clientId,
        BalancerClient balancerClient,
        RouterClient routerClient,
        ILogger logger)
    {
        _clientId = clientId;
        _balancerClient = balancerClient;
        _routerClient = routerClient;
        _routerClient.UnknownRecipient += OnUnknownRecipient;
        _routerClient.SetIndividualMessageHandler(OnIndividualMessage);
        _routerClient.SetGroupMessageHandler(OnGroupMessage);
        _routerClient.GroupSubscribeResponse += OnGroupSubscribeResponse;
        _logger = logger.ForContext(nameof(RopuClient));
    }

    void OnGroupSubscribeResponse(object? sender, EventArgs e)
    {
        _groupsSubscribed.Value = true;
        while (_subscribeCompletions.TryTake(out var subscribeCompletions))
        {
            subscribeCompletions.SetResult();
        }
    }

    MessageHandler? _individualMessageHandler;
    MessageHandler? _groupMessageHandler;

    public void SetIndividualMessageHandler(MessageHandler? handler)
    {
        _individualMessageHandler = handler;
    }

    public void SetGroupMessageHandler(MessageHandler? handler)
    {
        _groupMessageHandler = handler;
    }

    void OnIndividualMessage(Guid fromClientId, Guid toClientId, Span<byte> message)
    {
        _individualMessageHandler?.Invoke(fromClientId, toClientId, message);
    }

    void OnGroupMessage(Guid fromClientId, Guid groupId, Span<byte> message)
    {
        _groupMessageHandler?.Invoke(fromClientId, groupId, message);
    }

    public Guid UnitId => _clientId;

    void OnUnknownRecipient(object? sender, Guid clientId)
    {
        // Remove from lookup, the send will have failed but we will rely
        // on application level retries to trigger a address resolution
        _addressLookup.TryRemove(clientId, out SocketAddress? value);
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.Information($"RopuClient RunAsync");

        var routerClientTask = _routerClient.RunReceiveAsync(cancellationToken);
        var balancerClientTask = _balancerClient.RunReceiveAsync(cancellationToken);
        var receiveTask = _routerClient.RunReceiveAsync(cancellationToken);
        var manageConnectionTask = Task.Run(() => ManageConnection(cancellationToken));
        return TaskHelpers.RunTasksAsync(routerClientTask, balancerClientTask, manageConnectionTask);
    }

    readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(25);

    public void ManageConnection(CancellationToken cancellationToken)
    {
        _logger.Information($"ManageConnection called");

        while (!cancellationToken.IsCancellationRequested)
        {
            var routerAddress = _balancerClient.Register(cancellationToken);
            _logger.Information($"Assigned to router {routerAddress}");
            _routerClient.RouterAddress = routerAddress;
            if (!_routerClient.Register(_clientId))
            {
                IsConnected = false;
                continue;
            }
            _logger.Debug("Setting IsConnected to true");

            IsConnected = true;
            _groupsSubscribed.Value = false;


            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_routerClient.SendHeartbeat())
                {
                    _logger.Debug("SendHeartbeat failed");
                    break;
                }
                Thread.Sleep(_heartbeatInterval);
                lock (_groupsLock)
                {
                    if (!_groupsSubscribed.Value)
                    {
                        _routerClient.SendSubscribeGroups(_groupGuids.AsSpan(_groups.Count));
                    }
                }
            }
            IsConnected = false;
        }
    }

    int _connectedFlag = 0;

    public bool IsConnected
    {
        get => Interlocked.CompareExchange(ref _connectedFlag, 0, int.MaxValue) == 1;
        private set
        {
            bool oldValue = Interlocked.Exchange(ref _connectedFlag, value ? 1 : 0) == 1;
            if (value == oldValue)
            {
                return;
            }
            if (value)
            {
                _logger.Debug("Raising Connected event");
                Connected?.Invoke(this, EventArgs.Empty);
                return;
            }
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task<bool> SendToUnit(Guid unitId, Memory<byte> data)
    {
        if (!_addressLookup.TryGetValue(unitId, out SocketAddress? routerAddress))
        {
            // resolve unit
            routerAddress = new(AddressFamily.InterNetwork);
            if (!await _balancerClient.TryResolveUnitAsync(unitId, routerAddress))
            {
                _logger.Warning($"Unable to resolve unit {unitId.ToString()}");
                return false;
            }

            _addressLookup[unitId] = routerAddress;
        }
        _routerClient.SendToClient(_clientId, unitId, routerAddress, data.Span);
        return true;
    }

    public void SendToGroup(Guid groupId, GroupMessageType messageType, Memory<byte> data)
    {
        _routerClient.SendToGroup(_clientId, groupId, messageType, data.Span);
    }

    readonly Guid[] _groupGuids = new Guid[2000];
    List<Group> _groups = new();
    readonly ThreadSafeBool _groupsSubscribed = new();
    readonly object _groupsLock = new object();


    public void SubscribeGroups(List<Group> groups)
    {
        if (groups.Count > _groupGuids.Length)
        {
            throw new ArgumentException($"To many groups, limit is {_groupGuids.Length}");
        }
        lock (_groupsLock)
        {
            _groupsSubscribed.Value = false;
            _groups = groups;
            int index = 0;
            foreach (var group in groups)
            {
                _groupGuids[index] = group.Guid;
                index++;
            }
        }

        _routerClient.SendSubscribeGroups(_groupGuids.AsSpan(0, _groups.Count));
    }

    ConcurrentBag<TaskCompletionSource> _subscribeCompletions = new();

    public async Task<bool> WaitForSubscribeGroups()
    {
        var completionSource = new TaskCompletionSource();
        _subscribeCompletions.Add(completionSource);

        if (_groupsSubscribed.Value)
        {
            return true;
        }
        var completedTask = await Task.WhenAny(completionSource.Task, Task.Delay(2000));
        if (completedTask == completionSource.Task)
        {
            return true;
        }
        return false;
    }

    public void SendGroupMessage(Group group, Span<byte> payload)
    {
        _routerClient.SendGroupMessage(_clientId, group.Guid, Protocol.GroupMessageType.OneOff, payload);
    }
}