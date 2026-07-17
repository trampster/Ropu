using System.Diagnostics;
using System.Net;
using Ropu.Protocol;
using Ropu.IntergrationTests;
using Ropu.Logging;
using Ropu.Router;
using Ropu.Shared;
using System.Security.Cryptography;

namespace IntegrationTests;

public class BalancerTests
{

    [Test]
    public void Routers_NothingConnected_NoRouters()
    {
        // arrange
        using var system = new TestSystem(0, 0, 0, new Logger(LogLevel.Debug));
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
        using var system = new TestSystem(1, 0, 0, new Logger(LogLevel.Debug));
        system.Start();

        // act
        var routers = system.Balancer.Routers;

        // assert
        Assert.IsTrue(await WaitFor(() => routers.Count() == 1, TimeSpan.FromSeconds(1)));

        var router = routers.Span[0];
        Assert.That(router.Capacity, Is.EqualTo(100));
        Assert.That(router.NumberRegistered, Is.EqualTo(0));
        system.Stop();
    }

    [Test]
    public async Task Clients_Two_BothRegisterWithDifferentRouters()
    {
        using var system = new TestSystem(2, 2, 0, new Logger(LogLevel.Debug));

        var clients = system.Clients;

        var client0 = clients[0].Service;
        var client1 = clients[1].Service;

        TaskCompletionSource client0Registered = new();
        client0.Connected += (sender, args) => client0Registered.SetResult();

        TaskCompletionSource client1Registered = new();
        client1.Connected += (sender, args) => client1Registered.SetResult();

        system.Start();


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
        using var system = new TestSystem(2, 2, 0, new Logger(LogLevel.Debug));
        system.Start();

        var clients = system.Clients;

        await system.WaitForClientsToConnect();

        var client0 = clients[0].Service;
        var client1 = clients[1].Service;

        byte[] messageBuffer = new byte[1024];
        int messageLength = 0;
        TaskCompletionSource messageReceived = new();
        Guid fromId = Guid.Empty;
        Guid toId = Guid.Empty;

        client1.SetIndividualMessageHandler((fromClientId, toClientId, message) =>
        {
            messageLength = message.Length;
            message.CopyTo(messageBuffer);
            fromId = fromClientId;
            toId = toClientId;
            messageReceived.SetResult();
        });

        var expectedMessage = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // act
        await client0.SendToUnit(client1.UnitId, expectedMessage.AsMemory());

        // assert
        Assert.That(await messageReceived.Task.WaitOneAsync(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(messageBuffer.AsSpan(0, messageLength).ToArray(), Is.EquivalentTo(expectedMessage));
        Assert.That(fromId, Is.EqualTo(client0.UnitId));
        Assert.That(toId, Is.EqualTo(client1.UnitId));

        system.Stop();
    }

    [Test]
    public async Task Clients_Three_CanSendGroupMessage()
    {
        using var system = new TestSystem(2, 3, 2, new Logger(LogLevel.Debug));
        system.Start();

        try
        {
            // arrange

            var clients = system.Clients;

            await system.WaitForClientsToConnect();

            var client0 = clients[0].Service;
            var client1 = clients[1].Service;
            var client2 = clients[2].Service;

            var group = new Ropu.Client.Group()
            {
                Name = "Group1",
                Guid = Guid.NewGuid()
            };

            foreach (var client in clients)
            {
                client.Service.SubscribeGroups([group]);
                Assert.That(await client.Service.WaitForSubscribeGroups(), "SubscribeGroups timed out");
            }

            byte[] messageBuffer1 = new byte[1024];
            int messageLength1 = 0;
            TaskCompletionSource messageReceived1 = new();
            Guid fromId1 = Guid.Empty;
            Guid toId1 = Guid.Empty;

            client1.SetGroupMessageHandler((fromUintId, toGroupId, message) =>
            {
                messageLength1 = message.Length;
                message.CopyTo(messageBuffer1);
                fromId1 = fromUintId;
                toId1 = toGroupId;
                messageReceived1.SetResult();
            });

            byte[] messageBuffer2 = new byte[1024];
            int messageLength2 = 0;
            TaskCompletionSource messageReceived2 = new();
            Guid fromId2 = Guid.Empty;
            Guid toId2 = Guid.Empty;

            client2.SetGroupMessageHandler((fromUintId, toGroupId, message) =>
            {
                messageLength2 = message.Length;
                message.CopyTo(messageBuffer2);
                fromId2 = fromUintId;
                toId2 = toGroupId;
                messageReceived2.SetResult();
            });

            var expectedMessage = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // act
            client0.SendToGroup(group.Guid, GroupMessageType.OneOff, expectedMessage.AsMemory());

            // assert
            Assert.That(await messageReceived1.Task.WaitOneAsync(TimeSpan.FromSeconds(5)), Is.True, "client1 didn't receive group message");
            Assert.That(messageBuffer1.AsSpan(0, messageLength1).ToArray(), Is.EquivalentTo(expectedMessage));
            Assert.That(fromId1, Is.EqualTo(client0.UnitId));
            Assert.That(toId1, Is.EqualTo(group.Guid));

            Assert.That(await messageReceived2.Task.WaitOneAsync(TimeSpan.FromSeconds(5)), Is.True, "client2 didn't receive group message");
            Assert.That(messageBuffer2.AsSpan(0, messageLength2).ToArray(), Is.EquivalentTo(expectedMessage));
            Assert.That(fromId2, Is.EqualTo(client0.UnitId));
            Assert.That(toId2, Is.EqualTo(group.Guid));
        }
        finally
        {
            system.Stop();
        }
    }

    [Test]
    public async Task Clients_Three_CanStreamSendGroupMessage()
    {
        using var system = new TestSystem(2, 3, 2, new Logger(LogLevel.Debug));
        system.Start();

        try
        {
            // arrange

            var clients = system.Clients;

            await system.WaitForClientsToConnect();

            var client0 = clients[0].Service;
            var client1 = clients[1].Service;
            var client2 = clients[2].Service;

            var group = new Ropu.Client.Group()
            {
                Name = "Group1",
                Guid = Guid.NewGuid()
            };

            foreach (var client in clients)
            {
                client.Service.SubscribeGroups([group]);
                Assert.That(await client.Service.WaitForSubscribeGroups(), "SubscribeGroups timed out");
            }

            List<(Guid FromId, Guid ToGroupId, byte[] Message)> client1ReceivedMessages = new();
            TaskCompletionSource messagesReceived1 = new();

            const int streamLength = 100;

            object listLock = new();

            client1.SetGroupMessageHandler((fromUintId, toGroupId, message) =>
            {
                lock (listLock)
                {
                    client1ReceivedMessages.Add((fromUintId, toGroupId, message.ToArray()));
                    if (client1ReceivedMessages.Count == streamLength)
                    {
                        messagesReceived1.SetResult();
                    }
                }
            });


            List<(Guid FromId, Guid ToGroupId, byte[] Message)> client2ReceivedMessages = new();
            TaskCompletionSource messagesReceived2 = new();

            client2.SetGroupMessageHandler((fromUintId, toGroupId, message) =>
            {
                lock (listLock)
                {
                    client2ReceivedMessages.Add((fromUintId, toGroupId, message.ToArray()));
                    if (client2ReceivedMessages.Count == streamLength)
                    {
                        messagesReceived2.SetResult();

                    }
                }
            });

            var expectedMessage = new byte[1];

            // act
            for (int index = 0; index < streamLength; index++)
            {
                expectedMessage[0] = (byte)index;
                client0.SendToGroup(group.Guid, GroupMessageType.Stream, expectedMessage.AsMemory());
            }

            // assert
            Assert.That(await messagesReceived1.Task.WaitOneAsync(TimeSpan.FromSeconds(10)), Is.True, $"client1 didn't receive group message stream, messages {client1ReceivedMessages.Count}");
            Assert.That(await messagesReceived2.Task.WaitOneAsync(TimeSpan.FromSeconds(10)), Is.True, $"client2 didn't receive group message stream, messages {client2ReceivedMessages.Count}");

            lock (listLock)
            {
                for (int index = 0; index < streamLength; index++)
                {
                    Assert.That(client1ReceivedMessages[index].FromId, Is.EqualTo(client0.UnitId));
                    Assert.That(client1ReceivedMessages[index].ToGroupId, Is.EqualTo(group.Guid));
                    Assert.That(client1ReceivedMessages[index].Message.Length, Is.EqualTo(1));
                    Assert.That(client1ReceivedMessages[index].Message[0], Is.EqualTo(index));

                    Assert.That(client2ReceivedMessages[index].FromId, Is.EqualTo(client0.UnitId));
                    Assert.That(client2ReceivedMessages[index].ToGroupId, Is.EqualTo(group.Guid));
                    Assert.That(client2ReceivedMessages[index].Message.Length, Is.EqualTo(1));
                    Assert.That(client2ReceivedMessages[index].Message[0], Is.EqualTo(index));
                }
            }
        }
        finally
        {
            system.Stop();
        }
    }

    [Test]
    public async Task Routers_Nine_RouterLearnsOfDistributors()
    {
        // arrange
        using var system = new TestSystem(9, 2, 2, new Logger(LogLevel.Debug));


        var routerCompletionList = new List<(RouterService Router, TaskCompletionSource CompletionSource)>();
        foreach (var routerServiceInstance in system.Routers)
        {
            var router = routerServiceInstance.Service;

            TaskCompletionSource gotTwoDistributors = new();

            router.DistributorsChanged += (sender, args) =>
            {
                if (router.Distributors.Length == system.Distributors.Count)
                {
                    gotTwoDistributors.SetResult();
                }
            };

            routerCompletionList.Add((router, gotTwoDistributors));
        }

        // act
        system.Start();

        // assert
        foreach (var routerCompletion in routerCompletionList)
        {
            Assert.That(await routerCompletion.CompletionSource.Task.WaitOneAsync(TimeSpan.FromSeconds(5)), Is.True);
            var distributors = routerCompletion.Router.Distributors.ToArray();

            Assert.That(distributors.Length, Is.EqualTo(2));

            foreach (var distributor in system.Distributors)
            {
                Assert.That(distributors, Has.One.Matches<SocketAddress>(d => d.GetPort() == distributor.Service.Port));
            }
        }

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