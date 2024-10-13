using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Ropu.BalancerProtocol;
using Ropu.Logging;

namespace Ropu.Router;

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

    //must only be used from the receive
    readonly SocketAddress[] _routers = new SocketAddress[2000];

    public BalancerClient(
        ILogger logger,
        IPEndPoint routerIpEndpoint,
        IPEndPoint balancerEndpoint,
        ushort capacity)
    {
        _logger = logger;
        logger.ForContext(nameof(BalancerClient));
        _balancerEndpoint = balancerEndpoint.Serialize();

        _registerMessage = new byte[11];

        var routerAddress = routerIpEndpoint.Serialize();
        _logger.Information($"Router Endpoint {routerAddress}");
        _balancerPacketFactory.BuildRegisterRouterPacket(_registerMessage, routerAddress, capacity);

        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, routerIpEndpoint.Port);
        _socket.Bind(endpoint);

        for (int index = 0; index < _routers.Length; index++)
        {
            _routers[index] = new SocketAddress(AddressFamily.InterNetwork);
        }
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

                StartRouterSync();
                DoHeartBeat();
            }
            else
            {
                _logger.Information("Registration timed out after 2000 ms");
            }
        }
    }

    int _routerSyncPage = 0;

    void StartRouterSync()
    {
        _logger.Information("Starting Router Sync");

        _routerSyncPage = Interlocked.Exchange(ref _routerSyncPage, 1);
        var pageInfoRequest = _balancerPacketFactory.BuildRouterInfoPageRequest(
            _connectionManagementBuffer,
            1);
        _socket.SendTo(pageInfoRequest, SocketFlags.None, _balancerEndpoint);
    }

    // should only be used from the receive thread
    SocketAddress _tempSocketAddress = new SocketAddress(AddressFamily.InterNetwork);

    void HandleRouterInfoPage(Span<byte> buffer)
    {
        _logger.Debug("Received Header Info Page");

        if (!_balancerPacketFactory.TryParseRouterInfoPage(buffer, out byte pageNumber, out Span<byte> routersBuffer))
        {
            return;
        }
        _logger.Debug($"Received Header Info Page Number {pageNumber}");

        int routerCount = routersBuffer.Length / 8;

        for (int index = 0; index < routerCount; index++)
        {
            if (_balancerPacketFactory.TryParseRouterInfo(
                routersBuffer.Slice(index * 8),
                out ushort routerId, _tempSocketAddress))
            {
                _routers[routerId].CopyFrom(_tempSocketAddress);
                _logger.Debug($"Got Router: {routerId} @ {_routers[routerId]}");

            }
        }
        if (pageNumber == 10)
        {
            _logger.Debug($"Finished router sync.");
            return;
        }
        byte nextPageNumber = (byte)(pageNumber + 1);
        Interlocked.Exchange(ref _routerSyncPage, nextPageNumber);
        var pageInfoRequest = _balancerPacketFactory.BuildRouterInfoPageRequest(
            _receiveThreadBuffer,
            nextPageNumber);
        _socket.SendTo(pageInfoRequest, SocketFlags.None, _balancerEndpoint);
    }

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
                    case (byte)BalancerPacketTypes.RegisterRouterResponse:
                        HandleRegisterRouterResponse(_receiveThreadBuffer.AsSpan(0, received));
                        break;
                    case (byte)BalancerPacketTypes.HeartbeatResponse:
                        _heartBeatResponseEvent.Set();
                        break;
                    case (byte)BalancerPacketTypes.RouterInfoPage:
                        HandleRouterInfoPage(_receiveThreadBuffer.AsSpan(0, received));
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

    readonly byte[] _connectionManagementBuffer = new byte[5];

    void DoHeartBeat()
    {
        while (true)
        {
            _logger.Information("Sending heartbeat");

            var heartbeat = _balancerPacketFactory.BuildHeartbeatPacket(
                _connectionManagementBuffer,
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