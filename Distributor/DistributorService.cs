using System.Net;
using System.Net.Sockets;
using Ropu.Logging;

namespace Ropu.Distributor;

public class DistributorService : IDisposable
{
    readonly BalancerClient _balancerClient;
    readonly ushort _port;

    public DistributorService(ushort port, ILogger logger)
    {
        _port = port;
        _balancerClient = new BalancerClient(
            logger,
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port),
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000),
            100);
    }

    public ushort Port => _port;

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

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _balancerClient.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}