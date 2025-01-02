using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ropu.Logging;
using Ropu.Shared;

namespace Ropu.Client;

public class RopuClient
{
    readonly uint _clientId;
    readonly BalancerClient _balancerClient;
    readonly RouterClient _routerClient;
    readonly ILogger _logger;

    readonly ConcurrentDictionary<uint, SocketAddress> _addressLookup = new();

    public RopuClient(
        uint clientId,
        BalancerClient balancerClient,
        RouterClient routerClient,
        ILogger logger)
    {
        _clientId = clientId;
        _balancerClient = balancerClient;
        _routerClient = routerClient;
        _routerClient.UnknownRecipient += OnUnknownRecipient;
        _routerClient.SetIndividualMessageHandler(OnIndividualMessage);
        _logger = logger.ForContext(nameof(RopuClient));
    }

    IndividualMessageHandler? _individualMessageHandler;

    public void SetIndividualMessageHandler(IndividualMessageHandler? handler)
    {
        _individualMessageHandler = handler;
    }

    void OnIndividualMessage(Span<byte> message)
    {
        _individualMessageHandler?.Invoke(message);
    }

    public uint UnitId => _clientId;

    void OnUnknownRecipient(object? sender, uint clientId)
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
            IsConnected = true;

            while (!cancellationToken.IsCancellationRequested && _routerClient.SendHeartbeat())
            {
                Thread.Sleep(_heartbeatInterval);
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
                Connected?.Invoke(this, EventArgs.Empty);
                return;
            }
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task<bool> SendToUnit(uint unitId, Memory<byte> data)
    {
        if (!_addressLookup.TryGetValue(unitId, out SocketAddress? routerAddress))
        {
            // resolve unit
            routerAddress = new(AddressFamily.InterNetwork);
            if (!await _balancerClient.TryResolveUnitAsync(unitId, routerAddress))
            {
                _logger.Warning($"Unable to resolve unit {unitId}");
                return false;
            }

            _addressLookup[unitId] = routerAddress;
        }
        _routerClient.SendToClient(unitId, routerAddress, data.Span);
        return true;
    }
}