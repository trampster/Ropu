using System.Net;
using Ropu.Balancer;
using Ropu.Client;
using Ropu.Logging;
using Ropu.Router.Runner;

namespace Ropu.IntergrationTests;

public class ServiceInstance<T>
{
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly Func<T, CancellationToken, Task> _starter;
    T _service;
    Task _task = Task.CompletedTask;

    public ServiceInstance(Func<T, CancellationToken, Task> starter, T service)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _starter = starter;
        _service = service;
    }

    public T Service => _service;

    public void Start()
    {
        _task = _starter(_service, _cancellationTokenSource.Token);
    }

    public Task Stop()
    {
        _cancellationTokenSource.Cancel();
        return _task;
    }

}

public class TestSystem
{
    readonly ServiceInstance<Listener> _balancerService;
    readonly List<ServiceInstance<RouterRunner>> _routers;
    readonly List<ServiceInstance<RopuClient>> _clients;

    public TestSystem(int routers, int clients, ILogger logger)
    {
        // balancer
        _balancerService = new ServiceInstance<Listener>(
            (balancer, cancelationToken) => balancer.RunAsync(cancelationToken),
            new Listener(logger, 2000));

        // routers
        _routers = new(routers);
        for (int index = 0; index < routers; index++)
        {
            var serviceInstance = new ServiceInstance<RouterRunner>(
                (routerRunner, cancelationToken) => routerRunner.Run(cancelationToken),
                new RouterRunner((ushort)(2001 + index), logger));
            _routers.Add(serviceInstance);
        }

        // clients
        var balancerEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.115"), 2000);
        _clients = new(clients);
        for (uint index = 0; index < clients; index++)
        {
            var balancerClient = new BalancerClient(0, balancerEndpoint, index, logger);
            var routerClient = new RouterClient(new(), logger);
            RopuClient ropuClient = new(index, balancerClient, routerClient, logger);
            var serviceInstance = new ServiceInstance<RopuClient>(
                (client, cancelationToken) => Task.CompletedTask,
                ropuClient);
            _clients.Add(serviceInstance);
        }
    }

    public Listener Balancer => _balancerService.Service;

    public void Start()
    {
        _balancerService.Start();

        foreach (var router in _routers)
        {
            router.Start();
        }

        foreach (var client in _clients)
        {
            client.Start();
        }
    }

    public void Stop()
    {
        _balancerService.Stop();

        foreach (var router in _routers)
        {
            router.Stop();
        }
    }
}