using System.Net;
using Ropu.Balancer;
using Serilog;

Console.WriteLine("Ropu Balancer");
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var balancerClient = new BalancerClient(
    0,
    new IPEndPoint(IPAddress.Parse("192.168.1.115"), 2000),
    logger);

var routerEndpoint = await balancerClient.Register();

logger.Information($"Assigned to router {routerEndpoint}");