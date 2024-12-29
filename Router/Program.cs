using Ropu.Logging;
using Ropu.Router;

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

using var routerRunner = new RouterService(port, logger);

await routerRunner.Run(new CancellationTokenSource().Token);

Console.WriteLine("Ropu Router Stopped");