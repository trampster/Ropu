using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ropu.BalancerProtocol;
using Ropu.Logging;

namespace Ropu.Router;

public class BalancerClient : IDisposable
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

        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, 0);
        _socket.Bind(endpoint);
    }

    void ManageConnection(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
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
                        _heartBeatResponseEvent.Set();
                        break;
                    default:
                        //unhandled message
                        break;
                }
            }
        }
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
        Interlocked.Exchange(ref _routerId, routerId);

        _registerResponseEvent.Set();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var taskFactory = new TaskFactory();
        cancellationToken.Register(() => _socket.Close());
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
                BalancerPacketTypes.RouterHeartbeat,
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