using System.Net;
using Ropu.Balancer;
using Ropu.Protocol;
using Ropu.Client;
using Ropu.Distributor;
using Ropu.Logging;
using Ropu.Router;

namespace Ropu.IntergrationTests;

public class ServiceInstance<T> : IDisposable
{
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly Func<T, CancellationToken, Task> _starter;
    T _service;
    Task _task = Task.CompletedTask;

    readonly ILogger _logger;

    public ServiceInstance(Func<T, CancellationToken, Task> starter, T service, ILogger logger)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _starter = starter;
        _service = service;
        _logger = logger.ForContext(nameof(ServiceInstance<T>));
    }

    public T Service => _service;

    public void Start()
    {
        _task = StartAsync();
    }

    async Task StartAsync()
    {
        try
        {
            await _starter(_service, _cancellationTokenSource.Token);
        }
        catch (Exception exception)
        {
            _logger.Warning($"Exception occured running ServiceInstance of type {typeof(T).ToString()}, {exception.ToString()}");
        }
    }

    public Task Stop()
    {
        _cancellationTokenSource.Cancel();
        return _task;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            var disposable = _service as IDisposable;
            disposable?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class TestSystem : IDisposable
{
    readonly ServiceInstance<Listener> _balancerService;
    readonly List<ServiceInstance<RouterService>> _routers;
    readonly List<ServiceInstance<DistributorService>> _distributors;
    readonly List<ServiceInstance<RopuClient>> _clients;

    public TestSystem(int routers, int clients, int distributors, ILogger logger)
    {
        // balancer
        _balancerService = new ServiceInstance<Listener>(
            (balancer, cancelationToken) => balancer.RunAsync(cancelationToken),
            new Listener(logger.ForModule("Balancer"), 2000),
            logger);

        // distributors
        _distributors = new(distributors);
        for (int index = 0; index < distributors; index++)
        {
            var serviceInstance = new ServiceInstance<DistributorService>(
                (routerRunner, cancelationToken) => routerRunner.Run(cancelationToken),
                new DistributorService((ushort)(10001 + index), logger.ForModule($"Distributor{index}")),
                logger);
            _distributors.Add(serviceInstance);
        }

        // routers
        _routers = new(routers);
        for (int index = 0; index < routers; index++)
        {
            var serviceInstance = new ServiceInstance<RouterService>(
                (routerRunner, cancelationToken) => routerRunner.Run(cancelationToken),
                new RouterService((ushort)(2001 + index), logger.ForModule($"Router{index}")),
                logger);
            _routers.Add(serviceInstance);
        }

        // clients
        var balancerEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
        _clients = new(clients);
        for (uint index = 0; index < clients; index++)
        {
            var clientLogger = logger.ForModule($"Client{index}");

            var clientId = Guid.NewGuid();
            var balancerClient = new Client.BalancerClient(0, balancerEndpoint, clientId, clientLogger);
            var routerClient = new RouterClient(new(), clientLogger);
            RopuClient ropuClient = new(clientId, balancerClient, routerClient, clientLogger);
            var serviceInstance = new ServiceInstance<RopuClient>(
                (client, cancelationToken) => client.RunAsync(cancelationToken),
                ropuClient,
                logger);
            _clients.Add(serviceInstance);
        }
    }

    public Listener Balancer => _balancerService.Service;

    public List<ServiceInstance<RouterService>> Routers => _routers;

    public List<ServiceInstance<RopuClient>> Clients => _clients;
    public List<ServiceInstance<DistributorService>> Distributors => _distributors;

    public void Start()
    {
        _balancerService.Start();

        foreach (var router in _routers)
        {
            router.Start();
        }

        foreach (var distributor in _distributors)
        {
            distributor.Start();
        }

        foreach (var client in _clients)
        {
            client.Start();
        }
    }

    public async Task WaitForClientsToConnect()
    {
        foreach (var clientServiceInstance in _clients)
        {
            var client = clientServiceInstance.Service;

            if (!client.IsConnected)
            {
                var connectedCompletion = new TaskCompletionSource();
                EventHandler onConnected = (sender, args) => connectedCompletion.SetResult();
                client.Connected += onConnected;
                Assert.That(await Task.WhenAny(connectedCompletion.Task, Task.Delay(5000)), Is.EqualTo(connectedCompletion.Task));
                client.Connected -= onConnected;
            }
        }
    }

    public void Stop()
    {
        _balancerService.Stop();

        foreach (var router in _routers)
        {
            router.Stop();
        }

        foreach (var client in _clients)
        {
            client.Stop();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _balancerService.Dispose();

            foreach (var router in _routers)
            {
                router.Dispose();
            }

            foreach (var client in _clients)
            {
                client.Dispose();
            }
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}