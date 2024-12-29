using System.Net;
using System.Net.Http.Headers;
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
    readonly SocketAddress _routerAddress = new(AddressFamily.InterNetwork);

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

    byte[] _receiveBuffer = new byte[1024];

    public Task RunReceiveAsync(CancellationToken cancellationToken)
    {
        var taskFactory = new TaskFactory();
        return taskFactory.StartNew(() => RunReceive(cancellationToken), TaskCreationOptions.LongRunning);
    }

    public void RunReceive(CancellationToken cancellationToken)
    {
        _logger.Warning("Client: starting RunRecieve");
        while (!cancellationToken.IsCancellationRequested)
        {
            var socketAddress = new SocketAddress(AddressFamily.InterNetworkV6);
            var received = _socket.ReceiveFrom(_receiveBuffer, SocketFlags.None, socketAddress);
            _logger.Warning($"Client: received bytes {received}");
            if (received != 0)
            {
                switch (_receiveBuffer[0])
                {
                    case (byte)BalancerPacketTypes.RouterAssignment:
                        HandleRouterAssignment(_receiveBuffer.AsSpan(0, received));
                        break;
                    default:
                        _logger.Warning($"Received unknown packet type: {_receiveBuffer[0]}");
                        break;
                }
            }
        }
    }

    readonly ManualResetEvent _routerAssignmentEvent = new(false);

    void HandleRouterAssignment(Span<byte> packet)
    {
        if (_packetFactory.TryParseRouterAssignmentPacket(packet, _routerAddress))
        {
            _logger.Information("parsed router assignment packet");
            _routerAssignmentEvent.Set();
            return;
        }
        _logger.Warning("Failed to parse router assignment packet");
    }

    public SocketAddress Register(CancellationToken cancellationToken)
    {
        byte[] buffer = Buffer;

        while (!cancellationToken.IsCancellationRequested)
        {
            var routerAssignmentPacket = _packetFactory.BuildRouterAssignmentRequestPacket(
                buffer,
                _clientId);

            _routerAssignmentEvent.Reset();

            _logger.Information("Sending router assignment request");
            _socket.SendTo(routerAssignmentPacket, _balancerEndpoint);

            if (_routerAssignmentEvent.WaitOne(TimeSpan.FromSeconds(2)))
            {
                _logger.Information("Got Router Address to use");
                return _routerAddress;
            }
            _logger.Information("Timeout trying to get router assignment");

        }
        throw new TaskCanceledException("Register task was cancelled");
    }

}