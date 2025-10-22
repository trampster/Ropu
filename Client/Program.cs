using System.Net;
using Ropu.Client;
using Ropu.Logging;
using Ropu.Protocol;

Console.WriteLine("Ropu Client");
var logger = new Logger(LogLevel.Debug);

var clientId = Guid.NewGuid();

var balancerClient = new BalancerClient(
    0,
    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000),
    clientId,
    logger);

var routerClient = new RouterClient(
    new RouterPacketFactory(),
    logger
);

var ropuClient = new RopuClient(clientId, balancerClient, routerClient, logger);

await ropuClient.RunAsync(new CancellationTokenSource().Token);

Console.WriteLine("Ropu Client exited");
