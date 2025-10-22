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
    readonly Socket _socket;
    readonly ILogger _logger;

    readonly BalancerPacketFactory _balancerPacketFactory = new();
    int _routerId = IdNotAssigned;
    const ushort IdNotAssigned = ushort.MaxValue;

    public BalancerClient(
        ILogger logger,
        IPEndPoint distributorIpEndpoint,
        IPEndPoint balancerEndpoint,
        ushort capacity)
    {
        _logger = logger;
        logger.ForContext(nameof(BalancerClient));
        _balancerEndpoint = balancerEndpoint.Serialize();

        _registerMessage = new byte[11];

        var distributorAddress = distributorIpEndpoint.Serialize();
        _logger.Information($"Distributor Endpoint {distributorAddress}");
        _balancerPacketFactory.BuildRegisterDistributorPacket(_registerMessage, distributorAddress, capacity);

        _balancerPacketFactory.BuildRequestDistributorList(_requestDistributorListMessage);

        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        for (int index = 0; index < _distributorsBuffer.Length; index++)
        {
            _distributorsBuffer[index] = new SocketAddress(AddressFamily.InterNetwork);
        }

        var endpoint = new IPEndPoint(IPAddress.Any, distributorIpEndpoint.Port);
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

    // should only be used from the receive thread
    SocketAddress _tempSocketAddress = new SocketAddress(AddressFamily.InterNetwork);

    ManualResetEvent _registerResponseEvent = new(false);
    ManualResetEvent _heartBeatResponseEvent = new(false);

    void RunReceive(CancellationToken cancellationToken)
    {
        try
        {
            SocketAddress socketAddress = new SocketAddress(AddressFamily.InterNetworkV6);

            while (!cancellationToken.IsCancellationRequested)
            {
                var received = _socket.ReceiveFrom(_receiveThreadBuffer, SocketFlags.None, socketAddress);
                if (received != 0)
                {
                    switch (_receiveThreadBuffer[0])
                    {
                        case (byte)PacketTypes.RegisterDistributorResponse:
                            _logger.Debug("Received Register response");
                            HandleRegisterDistributorResponse(_receiveThreadBuffer.AsSpan(0, received));
                            break;
                        case (byte)PacketTypes.BalancerHeartbeatResponse:
                            _logger.Debug("Received Heartbeat response");
                            _heartBeatResponseEvent.Set();
                            break;
                        case (byte)PacketTypes.DistributorList:
                            HandleDistributorList(_receiveThreadBuffer.AsSpan(0, received));
                            break;
                        default:
                            //unhandled message
                            break;

                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.Warning($"Exception occured in RunReceive {exception.ToString()}");
            throw;
        }
    }

    readonly SocketAddress[] _distributorsBuffer = new SocketAddress[2000];
    SocketAddressList _distributors = new SocketAddressList(2000);
    ushort _sequenceNumber = 0;

    void HandleDistributorList(Span<byte> packet)
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
            case DistributorChangeType.Full:
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
            _socket.SendTo(_requestDistributorListMessage, SocketFlags.None, _balancerEndpoint);
            return;
        }
        _sequenceNumber = sequenceNumber;
    }

    void HandleRegisterDistributorResponse(Span<byte> packet)
    {
        if (!_balancerPacketFactory.TryParseRegisterDsitributorResponsePacket(
            packet,
            out ushort routerId))
        {
            _logger.Warning("Failed to parse Register Router Response Packet");
            return;
        }
        Interlocked.Exchange(ref _routerId, routerId);

        _registerResponseEvent.Set();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => _socket.Close());
        var taskFactory = new TaskFactory();
        var receiveTask = taskFactory.StartNew(() => RunReceive(cancellationToken), TaskCreationOptions.LongRunning);
        var connectionTask = taskFactory.StartNew(() => ManageConnection(cancellationToken), TaskCreationOptions.LongRunning);
        var task = await Task.WhenAny(receiveTask, connectionTask);
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
                PacketTypes.DistributorHeartbeat,
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