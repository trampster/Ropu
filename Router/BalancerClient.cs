using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ropu.BalancerProtocol;
using Ropu.Logging;
using Ropu.Shared;

namespace Ropu.Router;

public class BalancerClient : IDisposable
{
    readonly byte[] _registerMessage;
    readonly byte[] _requestDistributorListMessage = new byte[1];
    readonly byte[] _receiveThreadBuffer = new byte[1024];
    readonly SocketAddress _balancerEndpoint;
    readonly Socket _socket;
    readonly ILogger _logger;

    readonly BalancerPacketFactory _balancerPacketFactory = new();
    int _routerId = IdNotAssigned;
    const ushort IdNotAssigned = ushort.MaxValue;

    public BalancerClient(
        ILogger logger,
        IPEndPoint routerIpEndpoint,
        IPEndPoint balancerEndpoint,
        ushort capacity)
    {
        _logger = logger.ForContext(nameof(BalancerClient));

        _balancerEndpoint = balancerEndpoint.Serialize();

        _registerMessage = new byte[11];

        var routerAddress = routerIpEndpoint.Serialize();
        _logger.Information($"Router Endpoint {routerAddress}");
        _balancerPacketFactory.BuildRegisterRouterPacket(_registerMessage, routerAddress, capacity);

        _balancerPacketFactory.BuildRequestDistributorList(_requestDistributorListMessage);

        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        for (int index = 0; index < _distributorsBuffer.Length; index++)
        {
            _distributorsBuffer[index] = new SocketAddress(AddressFamily.InterNetwork);
        }

        var endpoint = new IPEndPoint(IPAddress.Any, 0);
        _socket.Bind(endpoint);
    }

    void ManageConnection(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.Information($"Registering with {_balancerEndpoint}");

            _registerResponseEvent.Reset();

            _socket.SendTo(_registerMessage, SocketFlags.None, _balancerEndpoint);

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

    void RunReceive(CancellationToken cancellationToken)
    {
        SocketAddress socketAddress = new SocketAddress(AddressFamily.InterNetworkV6);

        while (!cancellationToken.IsCancellationRequested)
        {
            var received = _socket.ReceiveFrom(_receiveThreadBuffer, SocketFlags.None, socketAddress);
            if (received != 0)
            {
                switch (_receiveThreadBuffer[0])
                {
                    case (byte)BalancerPacketTypes.RegisterRouterResponse:
                        HandleRegisterRouterResponse(_receiveThreadBuffer.AsSpan(0, received));
                        break;
                    case (byte)BalancerPacketTypes.HeartbeatResponse:
                        _logger.Debug("Heartbeat response received");
                        _heartBeatResponseEvent.Set();
                        break;
                    case (byte)BalancerPacketTypes.DistributorList:
                        HandleDistributorList(_receiveThreadBuffer.AsSpan(0, received));
                        break;
                    default:
                        //unhandled message
                        break;
                }
            }
        }
    }

    readonly SocketAddress[] _distributorsBuffer = new SocketAddress[2000];
    readonly SocketAddressList _distributors = new SocketAddressList(2000);
    ushort _sequenceNumber = 0;

    public Span<SocketAddress> Distributors => _distributors.AsSpan();

    public event EventHandler DistributorsChanged;

    void HandleDistributorList(Span<byte> packet)
    {
        _logger.Debug("Received Distributor List");
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
                _logger.Debug($"Received Distributor List, Full {distributors.Length} distributors");
                _distributors.Clear();
                _distributors.AddRange(distributors);
                _sequenceNumber = sequenceNumber;
                DistributorsChanged?.Invoke(this, EventArgs.Empty);
                return;
            case DistributorChangeType.Added:
                _logger.Debug($"Received Distributor List, Added {distributors.Length} distributors");
                foreach (var distributor in distributors)
                {
                    _logger.Debug($"   Distributor {distributor}");
                }
                _distributors.AddRange(distributors);

                DistributorsChanged?.Invoke(this, EventArgs.Empty);
                break;
            case DistributorChangeType.Removed:
                _logger.Debug($"Received Distributor List, Removed {distributors.Length} distributors");
                _distributors.RemoveRange(distributors);
                DistributorsChanged?.Invoke(this, EventArgs.Empty);
                break;
            default:
                _logger.Debug($"Distributor ChangeType {changeType.ToString()} is not suported");

                throw new InvalidOperationException($"Distributor ChangeType {changeType} is not suported");
        }
        var expectedSequenceNumber = _sequenceNumber + 1;
        if (sequenceNumber != expectedSequenceNumber)
        {
            // missed an update request full list
            _socket.SendTo(_requestDistributorListMessage, SocketFlags.None, _balancerEndpoint);
            return;
        }
        _sequenceNumber = sequenceNumber;
    }

    void HandleRegisterRouterResponse(Span<byte> packet)
    {
        if (!_balancerPacketFactory.TryParseRegisterRouterResponsePacket(
            packet,
            out ushort routerId))
        {
            _logger.Warning("Failed to parse Register Router Response Packet");
            return;
        }

        _socket.SendTo(_requestDistributorListMessage, SocketFlags.None, _balancerEndpoint);
        Interlocked.Exchange(ref _routerId, routerId);

        _registerResponseEvent.Set();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var taskFactory = new TaskFactory();
        cancellationToken.Register(() => _socket.Close());
        var receiveTask = taskFactory.StartNew(() => RunReceive(cancellationToken), TaskCreationOptions.LongRunning);
        var connectionTask = taskFactory.StartNew(() => ManageConnection(cancellationToken), TaskCreationOptions.LongRunning);
        var task = TaskHelpers.RunTasksAsync(receiveTask, connectionTask);
        await task;
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
                BalancerPacketTypes.RouterHeartbeat,
                (ushort)_routerId,
                (ushort)_registeredUsers.Count);

            _heartBeatResponseEvent.Reset();

            _socket.SendTo(heartbeat, SocketFlags.None, _balancerEndpoint);

            if (!_heartBeatResponseEvent.WaitOne(2000))
            {
                _logger.Information("Heartbeat response not received after 2000ms");
                return;
            }
            Thread.Sleep(5000);
        }
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