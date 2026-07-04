using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ropu.Protocol;
using Ropu.Logging;

namespace Ropu.Distributor;

public class BalancerClient : IDisposable
{
    readonly byte[] _registerMessage;
    readonly byte[] _requestDistributorListMessage = new byte[1];
    readonly byte[] _receiveThreadBuffer = new byte[1024];
    readonly SocketAddress _balancerEndpoint;
    readonly RopuSocket _socket;
    readonly ILogger _logger;

    readonly BalancerPacketFactory _balancerPacketFactory = new();
    int _routerId = IdNotAssigned;
    const ushort IdNotAssigned = ushort.MaxValue;

    public BalancerClient(
        ILogger logger,
        IPEndPoint distributorIpEndpoint,
        IPEndPoint balancerEndpoint,
        RopuSocket ropuSocket,
        ushort capacity)
    {
        _socket = ropuSocket;
        _logger = logger;
        logger.ForContext(nameof(BalancerClient));
        _balancerEndpoint = balancerEndpoint.Serialize();

        _registerMessage = new byte[11];

        var distributorAddress = distributorIpEndpoint.Serialize();
        _logger.Information($"Distributor Endpoint {distributorAddress}");
        _balancerPacketFactory.BuildRegisterDistributorPacket(_registerMessage, distributorAddress, capacity);

        _balancerPacketFactory.BuildRequestDistributorList(_requestDistributorListMessage);

        for (int index = 0; index < _distributorsBuffer.Length; index++)
        {
            _distributorsBuffer[index] = new SocketAddress(AddressFamily.InterNetwork);
        }
    }

    public void ManageConnection(CancellationToken cancellationToken)
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

    // should only be used from the receive thread
    SocketAddress _tempSocketAddress = new SocketAddress(AddressFamily.InterNetwork);

    ManualResetEvent _registerResponseEvent = new(false);
    ManualResetEvent _heartBeatResponseEvent = new(false);

    public void HandleBalancerHeartbeatResponse()
    {
        _logger.Debug("Received Heartbeat response");
        _heartBeatResponseEvent.Set();
    }

    readonly SocketAddress[] _distributorsBuffer = new SocketAddress[2000];
    SocketAddressList _distributors = new SocketAddressList(2000);
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
            return;
        }

        switch (changeType)
        {
            case DistributorChangeType.FullList:
                _distributors.Clear();
                _distributors.AddRange(distributors);
                _sequenceNumber = sequenceNumber;
                return;
            case DistributorChangeType.Added:
                _distributors.AddRange(distributors);
                break;
            case DistributorChangeType.Removed:
                foreach (var distributor in distributors)
                {
                    _distributors.Remove(distributor);
                }
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

    public void HandleRegisterDistributorResponse(Span<byte> packet)
    {
        _logger.Debug("Received Register response");

        if (!_balancerPacketFactory.TryParseRegisterDistributorResponsePacket(
            packet,
            out ushort routerId))
        {
            _logger.Warning("Failed to parse Register Router Response Packet");
            return;
        }
        Interlocked.Exchange(ref _routerId, routerId);

        _registerResponseEvent.Set();
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
                PacketTypes.DistributorHeartbeat,
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