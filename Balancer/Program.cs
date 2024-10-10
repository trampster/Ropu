using Ropu.Balancer;
using Ropu.Logging;

Console.WriteLine("Ropu Balancer");
var logger = new Logger(LogLevel.Debug);

var listener = new Listener(
    logger,
    2000);

await listener.RunAsync();