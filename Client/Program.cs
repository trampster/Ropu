using System.Net;
using Ropu.Client;
using Ropu.Logging;
using Ropu.RouterProtocol;

Console.WriteLine("Ropu Client");
var logger = new Logger(LogLevel.Debug);

uint clientId = (uint)Random.Shared.Next();

var balancerClient = new BalancerClient(
    0,
    new IPEndPoint(IPAddress.Parse("192.168.1.115"), 2000),
    clientId,
    logger);

var routerClient = new RouterClient(
    new RouterPacketFactory(),
    logger
);

var ropuClient = new RopuClient(clientId, balancerClient, routerClient, logger);

await ropuClient.RunAsync(new CancellationTokenSource().Token);

Console.WriteLine("Ropu Client exited");
