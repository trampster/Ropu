using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using BalancerProtocol;
using Ropu.BalancerProtocol;
using Ropu.Logging;

namespace Ropu.Balancer;

public class Listener : IDisposable
{
    readonly byte[] _buffer = new byte[1024];
    readonly Socket _socket;
    readonly BalancerPacketFactory _balancerPacketFactory = new();
    readonly ILogger _logger;

    readonly byte[] _heartbeatResponse;
    readonly Servers _routers;
    readonly BulkSender _bulkSender;
    readonly Servers _distributors;
    readonly Dictionary<int, SocketAddress> _routerAssignments = new();

    public Listener(
        ILogger logger,
        ushort port)
    {
        _logger = logger.ForContext(nameof(Listener));

        _heartbeatResponse = _balancerPacketFactory.HeartbeatResponse;


        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, port);
        _socket.Bind(endpoint);

        _routers = new Servers("Router", _logger);
        _distributors = new Servers("Distributor", _logger);

        _bulkSender = new BulkSender(_socket);
    }

    public Servers Distributors => _distributors;

    public Servers Routers => _routers;

    void RunReceive(CancellationToken cancellationToken)
    {
        try
        {
            SocketAddress receivedAddress = new(AddressFamily.InterNetwork);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var lastLastSeenCheck = stopwatch.ElapsedMilliseconds;

            var allocated = GC.GetAllocatedBytesForCurrentThread();
            _logger.Debug($"Allocated {allocated}");

            while (!cancellationToken.IsCancellationRequested)
            {
                var allocated1 = GC.GetAllocatedBytesForCurrentThread();
                _logger.Debug($"Allocated {allocated}");

                var received = _socket.ReceiveFrom(_buffer, SocketFlags.None, receivedAddress);
                if (received != 0)
                {
                    switch (_buffer[0])
                    {
                        case (byte)BalancerPacketTypes.RegisterRouter:
                            HandleRegisterRouter(received, receivedAddress);
                            break;
                        case (byte)BalancerPacketTypes.RegisterDistributor:
                            HandleRegisterDistributor(received, receivedAddress);
                            break;
                        case (byte)BalancerPacketTypes.RouterHeartbeat:
                            HandleRouterHeartbeat(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        case (byte)BalancerPacketTypes.DistributorHeartbeat:
                            HandleDistributorHeartbeat(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        case (byte)BalancerPacketTypes.RouterAssignmentRequest:
                            HandleRouterAssignmentRequest(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        case (byte)BalancerPacketTypes.ResolveUnit:
                            HandleResolveUnit(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        default:
                            //unhandled message
                            break;
                    }
                }
                if (lastLastSeenCheck + 5000 < stopwatch.ElapsedMilliseconds)
                {
                    _routers.CheckLastSeen();
                    _distributors.CheckLastSeen();
                    lastLastSeenCheck = stopwatch.ElapsedMilliseconds;
                }
            }
        }
        catch (SocketException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }

    }

    void HandleRouterHeartbeat(Span<byte> packet, SocketAddress receivedEndPoint)
    {
        if (!_balancerPacketFactory.TryParseHeartbeatPacket(packet, out HeartbeatPacket? heartbeatPacket))
        {
            _logger.Warning("Failed to parse heartbeat packet");
            return;
        }

        if (!_routers.TryGet(heartbeatPacket.Value.Id, out Server? router))
        {
            _logger.Warning($"Failed to find Router for Heartbeat with Router ID {heartbeatPacket.Value.Id}");
            return;
        }

        _logger.Debug($"Heartbeat from Id: {router.Id}, NumberRegistered: {router.NumberRegistered}, Capacity {router.Capacity} ");
        router.NumberRegistered = heartbeatPacket.Value.NumberRegistered;
        router.Seen = true;
        _socket.SendTo(_heartbeatResponse, SocketFlags.None, receivedEndPoint);
    }

    void HandleDistributorHeartbeat(Span<byte> packet, SocketAddress receivedEndPoint)
    {
        if (!_balancerPacketFactory.TryParseHeartbeatPacket(packet, out HeartbeatPacket? heartbeatPacket))
        {
            _logger.Warning("Failed to parse heartbeat packet");
            return;
        }

        if (!_distributors.TryGet(heartbeatPacket.Value.Id, out Server? distributor))
        {
            _logger.Warning($"Failed to find Router for Heartbeat with Router ID {heartbeatPacket.Value.Id}");
            return;
        }

        _logger.Debug($"Heartbeat from Id: {distributor.Id}, NumberRegistered: {distributor.NumberRegistered}, Capacity {distributor.Capacity} ");
        distributor.NumberRegistered = heartbeatPacket.Value.NumberRegistered;
        distributor.Seen = true;
        _socket.SendTo(_heartbeatResponse, SocketFlags.None, receivedEndPoint);
    }

    void HandleRouterAssignmentRequest(Span<byte> buffer, SocketAddress receivedEndPoint)
    {
        _logger.Information("Got Router Assignment Request");

        if (!_balancerPacketFactory.TryParseRouterAssignmentRequestPacket(buffer, out int clientId))
        {
            return;
        }
        float smallestLoad = float.PositiveInfinity;
        var routers = _routers.ServersArray;
        Server? smallest = null;
        foreach (var router in _routers.ServersArray)
        {
            if (!router.IsUsed)
            {
                continue;
            }
            float loadLevel = router.NumberRegistered / (float)router.Capacity;
            if (loadLevel < smallestLoad)
            {
                smallestLoad = loadLevel;
                smallest = router;
            }
        }

        if (smallest == null)
        {
            // could not find router to use
            // TODO: inform client of failure
            return;
        }

        _routerAssignments[clientId] = smallest.Endpoint;

        _logger.Information($"Sending Router Assignment, {smallest.Endpoint}");
        smallest.NumberRegistered++;
        var packet = _balancerPacketFactory.BuildRouterAssignmentPacket(_buffer, smallest.Endpoint);
        _socket.SendTo(packet, SocketFlags.None, receivedEndPoint);
    }

    void HandleResolveUnit(Span<byte> buffer, SocketAddress receivedEndPoint)
    {
        _logger.Information("Got Resolve Unit request");

        if (!_balancerPacketFactory.TryParseResolveUnitPacket(buffer, out int clientId))
        {
            return;
        }

        bool success = _routerAssignments.TryGetValue(clientId, out SocketAddress? routerAddress);

        var packet = _balancerPacketFactory.BuildResolveUnitResponse(_buffer, success, clientId, routerAddress);
        _socket.SendTo(packet, SocketFlags.None, receivedEndPoint);
        return;
    }

    void HandleRegisterRouter(int recieved, SocketAddress receivedEndPoint)
    {
        var router = _routers.FindNextUnused();
        if (router == null)
        {
            //TODO: max capacity reached, need to tell router to back off
            return;
        }
        if (!_balancerPacketFactory.TryParseRouterRegisterPacket(_buffer.AsSpan(0, recieved), router.Endpoint, out ushort? capacity))
        {
            _logger.Warning($"Failed to parse RegisterRouter packet");
            return;
        }
        router.Capacity = capacity.Value;
        router.NumberRegistered = 0;
        router.IsUsed = true;

        var registerResponsePacket = _balancerPacketFactory.BuildRegisterRouterResponsePacket(_buffer, router.Id);
        _socket.SendTo(registerResponsePacket, SocketFlags.None, receivedEndPoint);
    }

    void HandleRegisterDistributor(int recieved, SocketAddress receivedEndPoint)
    {
        var distributor = _distributors.FindNextUnused();
        if (distributor == null)
        {
            //TODO: max capacity reached, need to tell router to back off
            return;
        }
        if (!_balancerPacketFactory.TryParseRegisterDistributorPacket(_buffer.AsSpan(0, recieved), distributor.Endpoint, out ushort? capacity))
        {
            _logger.Warning($"Failed to parse RegisterDistributor packet");
            return;
        }
        distributor.Capacity = capacity.Value;
        distributor.NumberRegistered = 0;
        distributor.IsUsed = true;

        var registerResponsePacket = _balancerPacketFactory.BuildRegisterDistributorResponsePacket(_buffer, distributor.Id);
        _socket.SendTo(registerResponsePacket, SocketFlags.None, receivedEndPoint);
    }

    SocketAddress[] _destinationsBuffer = new SocketAddress[2000];

    public Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => _socket.Close());
        var taskFactory = new TaskFactory();
        var receiveTask = taskFactory.StartNew(() => RunReceive(cancellationToken), TaskCreationOptions.LongRunning);

        return receiveTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _socket.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}