using System.Net;
using System.Net.Sockets;
using Ropu.BalancerProtocol;
using Serilog;

namespace Ropu.Balancer;

public class BalancerClient
{
    readonly byte[] _buffer = new byte[1024];
    readonly Socket _socket;
    readonly ILogger _logger;
    readonly IPEndPoint _balancerEndpoint;

    public BalancerClient(
        ushort port,
        IPEndPoint balancerEndpoint,
        ILogger logger)
    {
        _balancerEndpoint = balancerEndpoint;
        _logger = logger.ForContext<BalancerClient>();

        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, port);
        _socket.Bind(endpoint);
    }

    public async Task<IPEndPoint> Register()
    {
        var packetFactory = new BalancerPacketFactory();
        EndPoint recievedEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            _logger.Information("Sending router assignment request");
            await _socket.SendToAsync(packetFactory.RouterAssignmentRequest, _balancerEndpoint);

            CancellationTokenSource receiveCancellationSource = new();
            receiveCancellationSource.CancelAfter(2000);
            try
            {
                var result = await _socket.ReceiveFromAsync(_buffer, recievedEndPoint, receiveCancellationSource.Token);
                if (result.ReceivedBytes == 0)
                {
                    await Task.Delay(1000);
                    continue;
                }
                _logger.Information($"Received {(BalancerPacketTypes)_buffer[0]}");

                if (_buffer[0] == (byte)BalancerPacketTypes.RouterAssignment)
                {
                    _logger.Information("Received RouterAssignment");

                    if (!packetFactory.TryParseRouterAssignmentPacket(_buffer.AsSpan(0, result.ReceivedBytes), out IPEndPoint? routerEndpoint))
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    return routerEndpoint;
                }

            }
            catch (OperationCanceledException)
            {
                await Task.Delay(2000);
                continue;
            }
        }
    }

}