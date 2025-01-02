using System.Diagnostics;
using Ropu.IntergrationTests;
using Ropu.Logging;
using Ropu.Shared;

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

        var clients = system.Clients;

        var client0 = clients[0].Service;
        var client1 = clients[1].Service;

        TaskCompletionSource client0Registered = new();
        client0.Connected += (sender, args) => client0Registered.SetResult();

        TaskCompletionSource client1Registered = new();
        client1.Connected += (sender, args) => client1Registered.SetResult();

        // act
        var routers = system.Routers;

        // assert
        Assert.That(await Task.WhenAny(client0Registered.Task, Task.Delay(5000)), Is.EqualTo(client0Registered.Task), "client0 didn't register in time");
        Assert.That(await Task.WhenAny(client1Registered.Task, Task.Delay(5000)), Is.EqualTo(client1Registered.Task), "client1 didn't register in time");
        Assert.That(client0.IsConnected, Is.True);
        Assert.That(client1.IsConnected, Is.True);
        Assert.That(routers[0].Service.Clients.Count, Is.EqualTo(1));
        Assert.That(routers[1].Service.Clients.Count, Is.EqualTo(1));

        system.Stop();
    }

    [Test]
    public async Task Clients_Two_CanSendMessages()
    {
        using var system = new TestSystem(2, 2, new Logger(LogLevel.Debug));
        system.Start();

        var clients = system.Clients;

        await system.WaitForClientsToConnect();

        var client0 = clients[0].Service;
        var client1 = clients[1].Service;

        byte[] messageBuffer = new byte[1024];
        int messageLength = 0;

        TaskCompletionSource messageRecieved = new();

        client1.SetIndividualMessageHandler(message =>
        {
            messageLength = message.Length;
            message.CopyTo(messageBuffer);
            messageRecieved.SetResult();
        });

        var expectedMessage = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // act
        await client0.SendToUnit(client1.UnitId, expectedMessage.AsMemory());

        // assert
        Assert.That(await messageRecieved.Task.WaitOneAsync(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(messageBuffer.AsSpan(0, messageLength).ToArray(), Is.EquivalentTo(expectedMessage));

        system.Stop();
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