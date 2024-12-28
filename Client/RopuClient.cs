using Ropu.Logging;

namespace Ropu.Client;

public class RopuClient
{
    readonly uint _clientId;
    readonly BalancerClient _balancerClient;
    readonly RouterClient _routerClient;
    readonly ILogger _logger;

    public RopuClient(
        uint clientId,
        BalancerClient balancerClient,
        RouterClient routerClient,
        ILogger logger)
    {
        _clientId = clientId;
        _balancerClient = balancerClient;
        _routerClient = routerClient;
        _logger = logger.ForContext(nameof(RopuClient));
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => Run(cancellationToken));
    }

    readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(25);

    public void Run(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var routerAddress = _balancerClient.Register();
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
}