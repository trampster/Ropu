using System.Net;
using System.Net.Sockets;
using Ropu.Logging;
using Ropu.Protocol;

namespace Ropu.Distributor;

public class DistributorService : IDisposable
{
    readonly RopuSocket _socket;
    readonly BalancerClient _balancerClient;
    readonly RopuProtocol _ropuProtocol;
    readonly ushort _port;

    public DistributorService(ushort port, ILogger logger)
    {
        _port = port;
        _socket = new RopuSocket(port);

        _balancerClient = new BalancerClient(
            logger,
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port),
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000),
            _socket,
            100);

        _ropuProtocol = new RopuProtocol(
            _socket,
            _balancerClient,
            new RouterProtocolHandler(_socket, 1000, logger),
            logger);
    }

    public ushort Port => _port;

    public async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.Register(() => _socket.Close());
            var taskFactory = new TaskFactory();
            var receiveTask = taskFactory.StartNew(() => _ropuProtocol.RunReceive(cancellationToken), TaskCreationOptions.LongRunning);
            var connectionTask = taskFactory.StartNew(() => _balancerClient.ManageConnection(cancellationToken), TaskCreationOptions.LongRunning);
            var task = await Task.WhenAny(receiveTask, connectionTask);
            await task;
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