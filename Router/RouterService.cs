using System.Net;
using System.Net.Sockets;
using Ropu.Client;
using Ropu.Logging;

namespace Ropu.Router;

public class RouterService : IDisposable
{
    readonly BalancerClient _balancerClient;
    readonly RouterListener _routerListener;

    public RouterService(ushort port, ILogger logger)
    {
        _routerListener = new RouterListener(port, new(), logger);

        _balancerClient = new BalancerClient(
            logger,
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), port),
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000),
            100);
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

    public IReadOnlyDictionary<Guid, SocketAddress> Clients => _routerListener.Clients;

    public async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            var balancerTask = _balancerClient.RunAsync(cancellationToken);
            var routerListenerTask = _routerListener.RunReceiveAsync(cancellationToken);
            await RunTasksAsync(balancerTask, routerListenerTask);
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

    public Span<SocketAddress> Distributors => _balancerClient.Distributors;

    public event EventHandler DistributorsChanged
    {
        add
        {
            _balancerClient.DistributorsChanged += value;
        }
        remove
        {
            _balancerClient.DistributorsChanged -= value;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _balancerClient.Dispose();
            _routerListener.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}