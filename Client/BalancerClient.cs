using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Ropu.Protocol;
using Ropu.Logging;

namespace Ropu.Client;

public class BalancerClient
{
    [ThreadStatic]
    static byte[]? _buffer;
    readonly Socket _socket;
    readonly Guid _clientId;
    readonly ILogger _logger;
    readonly IPEndPoint _balancerEndpoint;
    readonly BalancerPacketFactory _packetFactory = new();
    readonly SocketAddress _routerAddress = new(AddressFamily.InterNetwork);

    public BalancerClient(
        ushort port,
        IPEndPoint balancerEndpoint,
        Guid clientId,
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

    /// <summary>
    /// Resolves the address of the router that can be used to contact the unit.
    /// </summary>
    /// <param name="unitId"></param>
    /// <param name="routerAddress"></param>
    /// <returns></returns>
    public Task<bool> TryResolveUnitAsync(Guid unitId, SocketAddress routerAddress)
        => Task.Run(() => TryResolveUnit(unitId, routerAddress));

    ResolveUnitTracker GetResolveUnitTracker(Guid unitId, SocketAddress routerAddress)
    {
        foreach (var tracker in _resolveUnitRequests)
        {
            if (tracker.TryUse(unitId, routerAddress))
            {
                return tracker;
            }
        }

        //all in use
        var newTracker = new ResolveUnitTracker(unitId, routerAddress);
        newTracker.TryUse(unitId, routerAddress);
        _resolveUnitRequests.Add(newTracker);
        return newTracker;
    }

    bool TryResolveUnit(Guid unitId, SocketAddress routerAddress)
    {
        var buffer = Buffer;
        var resolveUnitAddress = _packetFactory.BuildResolveUnit(buffer, unitId);

        var tracker = GetResolveUnitTracker(unitId, routerAddress);
        _socket.SendTo(resolveUnitAddress, _balancerEndpoint);

        if (!tracker.WaitForResponse(TimeSpan.FromSeconds(5)))
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
                    case (byte)PacketTypes.RouterAssignment:
                        HandleRouterAssignment(_receiveBuffer.AsSpan(0, received));
                        break;
                    case (byte)PacketTypes.ResolveUnitResponse:
                        HandleResolveUnitResponse(_receiveBuffer.AsSpan(0, received));
                        break;
                    default:
                        _logger.Warning($"Received unknown packet type: {_receiveBuffer[0]}");
                        break;
                }
            }
        }
    }

    readonly AutoResetEvent _resoleUnitResponse = new(false);

    class ResolveUnitTracker
    {
        public Guid UnitId { get; private set; }
        ManualResetEvent _responseRecievedEvent = new(false);

        int _usedFlag = 0;

        public ResolveUnitTracker(Guid unitId, SocketAddress routerAddress)
        {
            UnitId = unitId;
            RouterAddress = routerAddress;
        }

        public bool TryUse(Guid unitId, SocketAddress routerAddress)
        {
            if (Interlocked.CompareExchange(ref _usedFlag, 1, 0) != 0)
            {
                return false;
            }
            UnitId = unitId;
            RouterAddress = routerAddress;
            _responseRecievedEvent.Reset();
            return true;
        }

        public bool WaitForResponse(TimeSpan timeout) => _responseRecievedEvent.WaitOne(timeout);

        public SocketAddress RouterAddress
        {
            get;
            set;
        }

        public void SetDone()
        {
            _responseRecievedEvent.Set();
        }

        public void Release()
        {
            Interlocked.Exchange(ref _usedFlag, 0);
        }
    }

    List<ResolveUnitTracker> _resolveUnitRequests = new();

    void HandleResolveUnitResponse(Span<byte> span)
    {
        SocketAddress socketAddress = new SocketAddress(AddressFamily.InterNetwork);
        if (!_packetFactory.TryParseResolveUnitResponseResult(
            span,
            out bool success,
            out Guid resolvedUnitId))
        {
            _logger.Warning("Failed to parse Resolve Unit Response packet");
            return;
        }
        foreach (var resolveUnitRequest in _resolveUnitRequests)
        {
            if (resolveUnitRequest.UnitId == resolvedUnitId)
            {
                _packetFactory.ParseResolveUnitResponseSocketAddress(span, resolveUnitRequest.RouterAddress);
                resolveUnitRequest.SetDone();
                return;
            }
        }
        _resoleUnitResponse.Set();
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