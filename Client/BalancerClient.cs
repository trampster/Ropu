using System.Net;
using System.Net.Sockets;
using Ropu.BalancerProtocol;
using Ropu.Logging;

namespace Ropu.Client;

public class BalancerClient
{
    [ThreadStatic]
    static byte[]? _buffer;
    readonly Socket _socket;
    readonly uint _clientId;
    readonly ILogger _logger;
    readonly IPEndPoint _balancerEndpoint;
    readonly BalancerPacketFactory _packetFactory = new();

    public BalancerClient(
        ushort port,
        IPEndPoint balancerEndpoint,
        uint clientId,
        ILogger logger)
    {
        _balancerEndpoint = balancerEndpoint;
        _clientId = clientId;
        _logger = logger.ForContext(nameof(BalancerClient));

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, port);
        _socket.Bind(endpoint);
    }

    static byte[] Buffer
    {
        get
        {
            if (_buffer == null)
            {
                _buffer = new byte[1024];
            }
            return _buffer;
        }
    }

    readonly SocketAddress _socketAddress = new SocketAddress(AddressFamily.InterNetwork);

    public Task<bool> TryResolveUnitAsync(int unitId, SocketAddress routerAddress)
        => Task.Run(() => TryResolveUnit(unitId, routerAddress));

    bool TryResolveUnit(int unitId, SocketAddress routerAddress)
    {
        var buffer = Buffer;
        var resolveUnitAddress = _packetFactory.BuildResolveUnit(buffer, unitId);
        _socket.SendTo(resolveUnitAddress, _balancerEndpoint);

        var receivedBytes = _socket.ReceiveFrom(buffer, SocketFlags.None, _socketAddress);

        if (!_packetFactory.TryParseResolveUnitResponse(
            buffer.AsSpan(0, receivedBytes),
            out bool success,
            out int resolvedUnitId,
            routerAddress))
        {
            _logger.Warning("Failed to parse Resolve Unit Response packet");
            return false;
        }

        if (resolvedUnitId != unitId)
        {
            _logger.Warning("Balancer resolved router address for the wrong unit");
            return false;
        }

        if (!success)
        {
            return false;
        }

        return true;
    }

    public Task RegisterAsync() => Task.Run(Register);

    public SocketAddress Register()
    {
        SocketAddress socketAddress = new SocketAddress(AddressFamily.InterNetwork);

        SocketAddress routerAddress = new(AddressFamily.InterNetwork);

        byte[] buffer = Buffer;

        while (true)
        {
            _logger.Information("Sending router assignment request");
            var routerAssignmentPacket = _packetFactory.BuildRouterAssignmentRequestPacket(
                buffer,
                _clientId);
            _socket.SendTo(routerAssignmentPacket, _balancerEndpoint);

            CancellationTokenSource receiveCancellationSource = new();
            receiveCancellationSource.CancelAfter(2000);
            try
            {
                var receivedBytes = _socket.ReceiveFrom(buffer, SocketFlags.None, socketAddress);
                if (receivedBytes == 0)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                _logger.Information($"Received {(BalancerPacketTypes)buffer[0]}");

                if (buffer[0] == (byte)BalancerPacketTypes.RouterAssignment)
                {
                    _logger.Information("Received RouterAssignment");

                    if (!_packetFactory.TryParseRouterAssignmentPacket(buffer.AsSpan(0, receivedBytes), routerAddress))
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    return routerAddress;
                }
            }
            catch (OperationCanceledException)
            {
                Thread.Sleep(2000);
                continue;
            }
        }
    }

}