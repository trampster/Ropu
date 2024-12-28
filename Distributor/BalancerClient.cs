using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ropu.BalancerProtocol;
using Ropu.Logging;

namespace Ropu.Distributor;

public class BalancerClient
{
    readonly byte[] _registerMessage;
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

        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, distributorIpEndpoint.Port);
        _socket.Bind(endpoint);
    }

    void ManageConnection()
    {
        while (true)
        {
            _logger.Information("Registering");
            _socket.SendTo(_registerMessage, SocketFlags.None, _balancerEndpoint);

            _registerResponseEvent.Reset();
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

    void RunReceive()
    {
        SocketAddress socketAddress = new SocketAddress(AddressFamily.InterNetworkV6);

        while (true)
        {
            var received = _socket.ReceiveFrom(_receiveThreadBuffer, SocketFlags.None, socketAddress);
            if (received != 0)
            {
                switch (_receiveThreadBuffer[0])
                {
                    case (byte)BalancerPacketTypes.RegisterDistributorResponse:
                        HandleRegisterDistributorResponse(_receiveThreadBuffer.AsSpan(0, received));
                        break;
                    case (byte)BalancerPacketTypes.HeartbeatResponse:
                        _heartBeatResponseEvent.Set();
                        break;
                    default:
                        //unhandled message
                        break;

                }
            }
        }
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

    public async Task RunAsync()
    {
        var taskFactory = new TaskFactory();
        var receiveTask = taskFactory.StartNew(RunReceive, TaskCreationOptions.LongRunning);
        var connectionTask = taskFactory.StartNew(ManageConnection, TaskCreationOptions.LongRunning);
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
                BalancerPacketTypes.DistributorHeartbeat,
                (ushort)_routerId,
                (ushort)_registeredUsers.Count);

            _socket.SendTo(heartbeat, SocketFlags.None, _balancerEndpoint);
            _heartBeatResponseEvent.Reset();
            if (!_heartBeatResponseEvent.WaitOne(2000))
            {
                _logger.Information("Heartbeat response not received after 2000ms");
                return;
            }
            Thread.Sleep(5000);
        }
    }
}