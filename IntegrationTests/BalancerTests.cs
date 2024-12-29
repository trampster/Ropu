using System.Diagnostics;
using Ropu.IntergrationTests;
using Ropu.Logging;

namespace IntegrationTests;

public class BalancerTests
{

    [Test]
    public void Routers_NothingConnected_NoRouters()
    {
        // arrange
        using var system = new TestSystem(0, 0, new Logger(LogLevel.Debug));
        system.Start();

        // act
        var routers = system.Balancer.Routers;

        // assert
        Assert.That(routers.Count(), Is.EqualTo(0));
        system.Stop();
    }

    [Test]
    public async Task Routers_OneConnected_HasRouter()
    {
        // arrange
        using var system = new TestSystem(1, 0, new Logger(LogLevel.Debug));
        system.Start();

        // act
        var routers = system.Balancer.Routers;

        // assert
        Assert.IsTrue(await WaitFor(() => routers.Count() == 1, TimeSpan.FromSeconds(1)));

        var router = routers.ServersArray.Where(router => router.IsUsed).First();
        Assert.That(router.Capacity, Is.EqualTo(100));
        Assert.That(router.NumberRegistered, Is.EqualTo(0));
        system.Stop();
    }

    [Test]
    public async Task Clients_Two_BothRegisterWithDifferentRouters()
    {
        using var system = new TestSystem(2, 2, new Logger(LogLevel.Debug));
        system.Start();

        // act
        var routers = system.Routers;

        // assert
        Assert.That(await WaitFor(() => routers[0].Service.Clients.Count == 1, TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(await WaitFor(() => routers[1].Service.Clients.Count == 1, TimeSpan.FromSeconds(5)), Is.True);
    }

    async Task<bool> WaitFor(Func<bool> outcome, TimeSpan waitTime)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (stopwatch.ElapsedMilliseconds < waitTime.TotalMilliseconds)
        {
            if (outcome())
            {
                return true;
            }
            await Task.Delay(20);
        }
        return false;
    }
}