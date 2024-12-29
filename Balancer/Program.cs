using Ropu.Balancer;
using Ropu.Logging;

Console.WriteLine("Ropu Balancer");
var logger = new Logger(LogLevel.Debug);

using var listener = new Listener(
    logger,
    2000);

var cancellationTokenSource = new CancellationTokenSource();

await listener.RunAsync(cancellationTokenSource.Token);