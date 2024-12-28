using System.Net;
using Ropu.Logging;
using Ropu.Router;
using Ropu.Router.Runner;

Console.WriteLine("Ropu Router");

var arguments = Environment.GetCommandLineArgs();
if (arguments.Length != 2)
{
    Console.Error.WriteLine($"Usage: router {{port}}");
    return;
}

string portString = arguments[1];
if (!ushort.TryParse(portString, out ushort port))
{
    Console.Error.WriteLine($"First argument must be the port but was {portString}");
    return;
}

var logger = new Logger(LogLevel.Debug);

var routerRunner = new RouterRunner(port, logger);

await routerRunner.Run(new CancellationTokenSource().Token);

Console.WriteLine("Ropu Router Stopped");