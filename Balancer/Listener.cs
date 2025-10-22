using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Ropu.Protocol;
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
    readonly Dictionary<Guid, SocketAddress> _routerAssignments = new();

    public Listener(
        ILogger logger,
        ushort port)
    {
        _logger = logger.ForContext(nameof(Listener));

        _logger.Debug($"Listening on port {port}");

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
        _logger.Debug("RunReceive");

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
                        case (byte)PacketTypes.RegisterRouter:
                            HandleRegisterRouter(received, receivedAddress);
                            break;
                        case (byte)PacketTypes.RegisterDistributor:
                            HandleRegisterDistributor(received, receivedAddress);
                            break;
                        case (byte)PacketTypes.RouterHeartbeat:
                            HandleRouterHeartbeat(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        case (byte)PacketTypes.DistributorHeartbeat:
                            HandleDistributorHeartbeat(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        case (byte)PacketTypes.RouterAssignmentRequest:
                            HandleRouterAssignmentRequest(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        case (byte)PacketTypes.ResolveUnit:
                            HandleResolveUnit(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        case (byte)PacketTypes.RequestDistributorList:
                            HandleRequestDistributorList(_buffer.AsSpan(0, received), receivedAddress);
                            break;
                        default:
                            //unhandled message
                            break;
                    }
                }
                if (lastLastSeenCheck + 5000 < stopwatch.ElapsedMilliseconds)
                {
                    _routers.CheckLastSeen(_serversBuffer);
                    var removed = _distributors.CheckLastSeen(_serversBuffer);
                    if (removed.Length > 0)
                    {
                        SendDistributorsRemoved(removed);
                    }

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
        catch (Exception exception)
        {
            _logger.Warning($"Exception {exception.ToString()}");
            throw;
        }
    }

    void HandleRequestDistributorList(Span<byte> span, SocketAddress receivedAddress)
    {
        var distributors = _distributors.Span;
        for (int index = 0; index < distributors.Length; index++)
        {
            if (receivedAddress != distributors[index].Address)
            {
                _socketAddressesBuffer[index] = distributors[index].Address;
            }
        }
        _logger.Warning($"Sending DistributorChange Full: {distributors.Length}");
        var packet = _balancerPacketFactory.BuildDistributorList(
            _buffer,
            _distributorSequenceNumber++,
            DistributorChangeType.Full,
            _socketAddressesBuffer.AsSpan(0, distributors.Length));

        _socket.SendTo(packet, SocketFlags.None, receivedAddress);
    }

    Server[] _serversBuffer = new Server[2000];
    SocketAddress[] _socketAddressesBuffer = new SocketAddress[2000];
    void SendDistributorsRemoved(Span<Server> removedServers)
    {
        for (int index = 0; index < removedServers.Length; index++)
        {
            _socketAddressesBuffer[index] = removedServers[index].Address;
        }
        var packet = _balancerPacketFactory.BuildDistributorList(
            _buffer,
            _distributorSequenceNumber++,
            DistributorChangeType.Removed,
            _socketAddressesBuffer.AsSpan(0, removedServers.Length));
        var packetMemory = _buffer.AsMemory(0, packet.Length);

        _logger.Warning($"Sending DistributorChange Removed: {removedServers.Length}");

        _bulkSender.SendBulk(packetMemory, _distributors.ReplyAddresses, null);
        _bulkSender.SendBulk(packetMemory, _routers.ReplyAddresses, null);
    }

    void HandleRouterHeartbeat(Span<byte> packet, SocketAddress receivedEndPoint)
    {
        _logger.Debug("Got Router Heartbeat");
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

        _logger.Debug($"Distributor Heartbeat received {distributor.Id}");
        distributor.NumberRegistered = heartbeatPacket.Value.NumberRegistered;
        distributor.Seen = true;
        _socket.SendTo(_heartbeatResponse, SocketFlags.None, receivedEndPoint);
    }

    void HandleRouterAssignmentRequest(Span<byte> buffer, SocketAddress receivedEndPoint)
    {
        _logger.Information("Got Router Assignment Request");

        if (!_balancerPacketFactory.TryParseRouterAssignmentRequestPacket(buffer, out Guid clientId))
        {
            return;
        }
        float smallestLoad = float.PositiveInfinity;
        Server? smallest = null;
        foreach (var router in _routers.Span)
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

        _routerAssignments[clientId] = smallest.Address;

        _logger.Information($"Sending Router Assignment, {smallest.Address}");
        smallest.NumberRegistered++;
        var packet = _balancerPacketFactory.BuildRouterAssignmentPacket(_buffer, smallest.Address);
        _socket.SendTo(packet, SocketFlags.None, receivedEndPoint);
    }

    void HandleResolveUnit(Span<byte> buffer, SocketAddress receivedEndPoint)
    {
        _logger.Information("Got Resolve Unit request");

        if (!_balancerPacketFactory.TryParseResolveUnitPacket(buffer, out Guid clientId))
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
        _logger.Debug("HandleRegisterRouter");
        var router = _routers.CheckExisting(receivedEndPoint);
        if (router == null)
        {
            router = _routers.FindNextUnused();
            if (router == null)
            {
                //TODO: max capacity reached, need to tell router to back off
                return;
            }
            if (!_balancerPacketFactory.TryParseRouterRegisterPacket(_buffer.AsSpan(0, recieved), router.Address, out ushort? capacity))
            {
                _logger.Warning($"Failed to parse RegisterRouter packet");
                return;
            }
            router.Capacity = capacity.Value;
            router.NumberRegistered = 0;
            router.ReplyAddress = receivedEndPoint;
            _routers.SetUsed(router);
        }

        var registerResponsePacket = _balancerPacketFactory.BuildRegisterRouterResponsePacket(_buffer, router.Id);
        _logger.Debug($"Sending register response to {receivedEndPoint}");
        _socket.SendTo(registerResponsePacket, SocketFlags.None, receivedEndPoint);
    }

    void HandleRegisterDistributor(int recieved, SocketAddress fromAddress)
    {
        _logger.Debug("RegisterDistributor packet received");
        var distributor = _distributors.FindNextUnused();
        if (distributor == null)
        {
            //TODO: max capacity reached, need to tell router to back off
            return;
        }
        if (!_balancerPacketFactory.TryParseRegisterDistributorPacket(_buffer.AsSpan(0, recieved), distributor.Address, out ushort? capacity))
        {
            _logger.Warning($"Failed to parse RegisterDistributor packet");
            return;
        }
        distributor.Capacity = capacity.Value;
        distributor.NumberRegistered = 0;
        distributor.ReplyAddress = fromAddress;
        _distributors.SetUsed(distributor);

        var registerResponsePacket = _balancerPacketFactory.BuildRegisterDistributorResponsePacket(_buffer, distributor.Id);

        _logger.Debug($"Sending Register response to {fromAddress}");
        _socket.SendTo(registerResponsePacket, SocketFlags.None, fromAddress);

        // Inform routers and existing distributors of new distributor
        _socketAddressesBuffer[0] = fromAddress;
        var distributorChangedPacket = _balancerPacketFactory.BuildDistributorList(
            _buffer,
            _distributorSequenceNumber++,
            DistributorChangeType.Added,
            _socketAddressesBuffer.AsSpan(0, 1));

        var packetMemory = _buffer.AsMemory(0, distributorChangedPacket.Length);

        _logger.Debug($"Sending DistributorChanged Added {_socketAddressesBuffer.AsSpan(0, 1)[0]}");

        _bulkSender.SendBulk(
            packetMemory,
            _routers.ReplyAddresses,
            null);

        _bulkSender.SendBulk(
            packetMemory,
            _distributors.ReplyAddresses,
            fromAddress);
    }

    ushort _distributorSequenceNumber = 0;

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