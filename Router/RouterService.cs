using System.Net;
using System.Net.Sockets;
using Ropu.Logging;
using Ropu.Protocol;
using Router.Router;

namespace Ropu.Router;

public class RouterService : IDisposable
{
    readonly RopuSocket _ropuSocket;
    readonly DistributorsManager _distributorsManager;
    readonly BalancerClient _balancerClient;
    readonly RopuProtocol _ropuProtocol;
    readonly RouterListener _routerListener;

    public RouterService(ushort port, ILogger logger)
    {
        _ropuSocket = new RopuSocket(port);

        _distributorsManager = new();


        _routerListener = new RouterListener(_ropuSocket, new(), _distributorsManager, logger);

        _balancerClient = new BalancerClient(
            logger,
            _ropuSocket,
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), port),
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000),
            _distributorsManager,
            100);

        _ropuProtocol = new RopuProtocol(_ropuSocket, _balancerClient, _routerListener, logger);
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

    public IReadOnlyDictionary<Guid, Client> Clients => _routerListener.Clients;

    public async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            var balancerTask = _balancerClient.RunAsync(cancellationToken);
            var protocolReceiveTask = _ropuProtocol.RunReceiveAsync(cancellationToken);
            await RunTasksAsync(balancerTask, protocolReceiveTask);
        }
        catch (SocketException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            throw;
        }
    }

    public Span<SocketAddress> Distributors => _distributorsManager.Distributors;

    public event EventHandler DistributorsChanged
    {
        add
        {
            _distributorsManager.DistributorsChanged += value;
        }
        remove
        {
            _distributorsManager.DistributorsChanged -= value;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ropuSocket.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}