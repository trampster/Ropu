using Ropu.Balancer;
using Serilog;

Console.WriteLine("Ropu Balancer");
var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var listener = new Listener(
    logger,
    2000);

var task = listener.RunAsync();

await Task.WhenAny(task);