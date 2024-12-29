using System.Net;
using Ropu.Distributor;
using Ropu.Logging;

Console.WriteLine("Ropu Distributor");

var arguments = Environment.GetCommandLineArgs();
if (arguments.Length != 2)
{
    Console.Error.WriteLine($"Usage: distributor {{port}}");
    return;
}

string portString = arguments[1];
if (!ushort.TryParse(portString, out ushort port))
{
    Console.Error.WriteLine($"First argument must be the port but was {portString}");
    return;
}

var logger = new Logger(LogLevel.Debug);

using var balancerClient = new BalancerClient(
    logger,
    new IPEndPoint(IPAddress.Parse("127.0.0.1"), port),
    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000),
    100);

await balancerClient.RunAsync(new());

Console.WriteLine("Ropu Distributor Stopped");