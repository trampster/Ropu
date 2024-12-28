using System.Net;
using System.Net.Sockets;
using Ropu.Logging;

namespace Ropu.Router.Runner;

public class RouterRunner
{
    readonly BalancerClient _balancerClient;

    public RouterRunner(ushort port, ILogger logger)
    {
        _balancerClient = new BalancerClient(
            logger,
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), port),
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000),
            100);
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            await _balancerClient.RunAsync(cancellationToken);
        }
        catch (SocketException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            throw;
        }
    }
}