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

    async Task RunTasksAsync(params Task[] tasks)
    {
        var taskList = tasks.ToList();
        while (taskList.Count != 0)
        {
            var task = await Task.WhenAny(taskList);
            await task;
            taskList.Remove(task);
        }
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.Information($"RopuClient RunAsync");

        var routerClientTask = _routerClient.RunReceiveAsync(cancellationToken);
        var balancerClientTask = _balancerClient.RunReceiveAsync(cancellationToken);
        var receiveTask = _routerClient.RunReceiveAsync(cancellationToken);
        var manageConnectionTask = Task.Run(() => ManageConnection(cancellationToken));
        return RunTasksAsync(routerClientTask, balancerClientTask, manageConnectionTask);
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
}