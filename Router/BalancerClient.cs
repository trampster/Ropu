using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ropu.Protocol;
using Ropu.Logging;

namespace Ropu.Router;

public class BalancerClient
{
    readonly byte[] _registerMessage;
    readonly byte[] _requestDistributorListMessage = new byte[1];
    readonly byte[] _receiveThreadBuffer = new byte[1024];
    readonly SocketAddress _balancerEndpoint;
    readonly RopuSocket _socket;
    readonly DistributorsManager _distributorsManager;
    readonly ILogger _logger;

    readonly BalancerPacketFactory _balancerPacketFactory = new();
    int _routerId = IdNotAssigned;
    const ushort IdNotAssigned = ushort.MaxValue;

    public BalancerClient(
        ILogger logger,
        RopuSocket socket,
        IPEndPoint routerIpEndpoint,
        IPEndPoint balancerEndpoint,
        DistributorsManager distributorsManager,
        ushort capacity)
    {
        _distributorsManager = distributorsManager;
        _logger = logger.ForContext(nameof(BalancerClient));

        _balancerEndpoint = balancerEndpoint.Serialize();

        _registerMessage = new byte[11];

        var routerAddress = routerIpEndpoint.Serialize();
        _logger.Information($"Router Endpoint {routerAddress}");
        _balancerPacketFactory.BuildRegisterRouterPacket(_registerMessage, routerAddress, capacity);

        _balancerPacketFactory.BuildRequestDistributorList(_requestDistributorListMessage);

        _socket = socket;

        for (int index = 0; index < _distributorsBuffer.Length; index++)
        {
            _distributorsBuffer[index] = new SocketAddress(AddressFamily.InterNetwork);
        }
    }

    void ManageConnection(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.Information($"Registering with {_balancerEndpoint}");

            _registerResponseEvent.Reset();

            _socket.SendTo(_registerMessage, _balancerEndpoint);

            if (_registerResponseEvent.WaitOne(2000))
            {
                _logger.Information("Registration Successful starting heartbeats");

                DoHeartBeat();
            }
            else
            {
                _logger.Information("Registration timed out after 2000 ms");
            }
        }
    }

    readonly ManualResetEvent _registerResponseEvent = new(false);
    readonly ManualResetEvent _heartBeatResponseEvent = new(false);

    readonly SocketAddress[] _distributorsBuffer = new SocketAddress[2000];
    ushort _sequenceNumber = 0;

    public void HandleDistributorList(Span<byte> packet)
    {
        if (!_balancerPacketFactory.TryParseDistributorList(
            packet,
            _distributorsBuffer,
            out ushort sequenceNumber,
            out DistributorChangeType changeType,
            out Span<SocketAddress> distributors))
        {
            _logger.Debug("Warning Failed to parse distributor list");
            return;
        }

        switch (changeType)
        {
            case DistributorChangeType.Full:
                _distributorsManager.ReplaceList(distributors);
                _sequenceNumber = sequenceNumber;
                return;
            case DistributorChangeType.Added:
                _distributorsManager.Add(distributors);
                break;
            case DistributorChangeType.Removed:
                _distributorsManager.Remove(distributors);
                break;
            default:
                throw new InvalidOperationException($"Distributor ChangeType {changeType} is not suported");
        }
        var expectedSequenceNumber = _sequenceNumber + 1;
        if (sequenceNumber != expectedSequenceNumber)
        {
            // missed an update request full list
            _socket.SendTo(_requestDistributorListMessage, _balancerEndpoint);
            return;
        }
        _sequenceNumber = sequenceNumber;
    }

    public void HandleBalancerHeartbeatResponse()
    {
        _heartBeatResponseEvent.Set();
    }

    public void HandleRegisterRouterResponse(Span<byte> packet)
    {
        _logger.Debug("HandleReigsterRouterReponse");
        if (!_balancerPacketFactory.TryParseRegisterRouterResponsePacket(
            packet,
            out ushort routerId))
        {
            _logger.Warning("Failed to parse Register Router Response Packet");
            return;
        }

        _socket.SendTo(_requestDistributorListMessage, _balancerEndpoint);
        Interlocked.Exchange(ref _routerId, routerId);

        _registerResponseEvent.Set();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var taskFactory = new TaskFactory();
        cancellationToken.Register(() => _socket.Close());
        await taskFactory.StartNew(() => ManageConnection(cancellationToken), TaskCreationOptions.LongRunning);
    }

    readonly ConcurrentDictionary<int, EndPoint> _registeredUsers = new();

    readonly byte[] _connectionManagementBuffer = new byte[5];

    void DoHeartBeat()
    {
        while (true)
        {
            _logger.Information("Sending heartbeat");

            var heartbeat = _balancerPacketFactory.BuildHeartbeatPacket(
                _connectionManagementBuffer,
                PacketTypes.RouterHeartbeat,
                (ushort)_routerId,
                (ushort)_registeredUsers.Count);

            _heartBeatResponseEvent.Reset();

            _socket.SendTo(heartbeat, _balancerEndpoint);

            if (!_heartBeatResponseEvent.WaitOne(2000))
            {
                _logger.Information("Heartbeat response not received after 2000ms");
                return;
            }
            Thread.Sleep(5000);
        }
    }
}