using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ropu.BalancerProtocol;
using Serilog;

namespace Ropu.Router;

public class BalancerClient
{
    readonly byte[] _registerMessage;
    readonly byte[] _buffer = new byte[1024];
    readonly IPEndPoint _routerIpEndpoint;
    readonly IPEndPoint _ballancerEndpoint;
    readonly Socket _socket;
    readonly ILogger _logger;

    readonly BalancerPacketFactory _balancerPacketFactory = new();
    int _routerId = IdNotAssigned;
    const ushort IdNotAssigned = ushort.MaxValue;

    public BalancerClient(
        ILogger logger,
        IPEndPoint routerIpEndpoint,
        IPEndPoint ballancerEndpoint,
        ushort capacity)
    {
        _logger = logger.ForContext<BalancerClient>();
        _ballancerEndpoint = ballancerEndpoint;
        _routerIpEndpoint = routerIpEndpoint;

        _registerMessage = new byte[11];


        _logger.Information($"Router Endpoint {routerIpEndpoint}");
        _balancerPacketFactory.BuildRegisterRouterPacket(_registerMessage, routerIpEndpoint, capacity);

        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, _routerIpEndpoint.Port);
        _socket.Bind(endpoint);
    }

    void ManageConnection()
    {
        while (true)
        {
            _logger.Information("Registering");
            _socket.SendTo(_registerMessage, _ballancerEndpoint);

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

    ManualResetEvent _registerResponseEvent = new ManualResetEvent(false);
    ManualResetEvent _heartBeatResponseEvent = new ManualResetEvent(false);

    void RunReceive()
    {
        EndPoint recievedEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            var received = _socket.ReceiveFrom(_buffer, ref recievedEndPoint);
            if (received != 0)
            {
                switch (_buffer[0])
                {
                    case (byte)BalancerPacketTypes.RegisterRouterResponse:
                        HandleRegisterRouterResponse(_buffer.AsSpan(0, received));
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

    public async Task RunAsync()
    {
        var taskFactory = new TaskFactory();
        var receiveTask = taskFactory.StartNew(RunReceive, TaskCreationOptions.LongRunning);
        var connectionTask = taskFactory.StartNew(ManageConnection, TaskCreationOptions.LongRunning);
        var task = await Task.WhenAny(receiveTask, connectionTask);
        await task;
    }

    readonly ConcurrentDictionary<int, EndPoint> _registeredUsers = new();

    readonly byte[] _heartbeatBuffer = new byte[5];

    void DoHeartBeat()
    {
        while (true)
        {
            _logger.Information("Sending heartbeat");

            var heartbeat = _balancerPacketFactory.BuildHeartbeatPacket(
                _heartbeatBuffer,
                (ushort)_routerId,
                (ushort)_registeredUsers.Count);

            _socket.SendTo(heartbeat, _ballancerEndpoint);
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