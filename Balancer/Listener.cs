using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using Ropu.BalancerProtocol;
using Ropu.Logging;

namespace Ropu.Balancer;

public class Listener
{
    readonly byte[] _buffer = new byte[1024];
    readonly Socket _socket;
    readonly BalancerPacketFactory _balancerPacketFactory = new();
    readonly ILogger _logger;

    readonly byte[] _heartbeatResponse;
    readonly Router[] _routers;

    public Listener(
        ILogger logger,
        ushort port)
    {
        logger.ForContext(nameof(Listener));
        _logger = logger;

        _heartbeatResponse = _balancerPacketFactory.HeartbeatResponse;


        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        var endpoint = new IPEndPoint(IPAddress.Any, port);
        _socket.Bind(endpoint);

        _routers = new Router[2000];
        for (int index = 0; index < _routers.Length; index++)
        {
            _routers[index] = new Router()
            {
                Id = (ushort)index
            };
        }
    }

    void RunReceive()
    {
        SocketAddress receivedAddress = new(AddressFamily.InterNetwork);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        var lastLastSeenCheck = stopwatch.ElapsedMilliseconds;

        char[] logBuffer = new char[1024];
        char[] defaultFormat = new char[0];


        var allocated = GC.GetAllocatedBytesForCurrentThread();
        _logger.Debug($"Allocated {allocated}");

        while (true)
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
                    case (byte)BalancerPacketTypes.Heartbeat:
                        HandleHeartbeat(_buffer.AsSpan(0, received), receivedAddress);
                        break;
                    case (byte)BalancerPacketTypes.RouterAssignmentRequest:
                        HandleRegisterAssignmentRequest(receivedAddress);
                        break;
                    case (byte)BalancerPacketTypes.RouterInfoPageRequest:
                        HandleRouterInfoPageRequest(_buffer.AsSpan(0, received), receivedAddress);
                        break;
                    default:
                        //unhandled message
                        break;
                }
            }
            if (lastLastSeenCheck + 5000 < stopwatch.ElapsedMilliseconds)
            {
                CheckLastSeen();
                lastLastSeenCheck = stopwatch.ElapsedMilliseconds;
            }
        }

    }

    void CheckLastSeen()
    {
        for (int index = 0; index < _routers.Length; index++)
        {
            var router = _routers[index];
            if (router.IsUsed && !router.Seen)
            {
                _logger.Information($"Router {router.Id} timed out removing");
                router.IsUsed = false;
            }
            else
            {
                router.Seen = false;
            }
        }
    }

    bool TryGetRouter(int routerId, [NotNullWhen(true)] out Router? router)
    {
        if (routerId >= _buffer.Length)
        {
            router = null;
            return false;
        }
        var routerAtIndex = _routers[routerId];
        if (routerAtIndex.IsUsed)
        {
            router = routerAtIndex;
            return true;
        }
        router = null;
        return false;
    }

    void HandleHeartbeat(Span<byte> packet, SocketAddress receivedEndPoint)
    {
        if (!_balancerPacketFactory.TryParseHeartbeatPacket(packet, out HeartbeatPacket? heartbeatPacket))
        {
            _logger.Warning("Failed to parse heartbeat packet");
            return;
        }

        if (!TryGetRouter(heartbeatPacket.Value.RouterId, out Router? router))
        {
            _logger.Warning($"Failed to find Router for Heartbeat with Router ID {heartbeatPacket.Value.RouterId}");
            return;
        }

        _logger.Debug($"Heartbeat from Id: {router.Id}, NumberRegistered: {router.NumberRegistered}, Capacity {router.Capacity} ");
        router.NumberRegistered = heartbeatPacket.Value.RegisteredUsers;
        router.Seen = true;
        _socket.SendTo(_heartbeatResponse, SocketFlags.None, receivedEndPoint);
    }

    void HandleRegisterAssignmentRequest(SocketAddress receivedEndPoint)
    {
        _logger.Information("Got Router Assignment Request");
        float smallestLoad = float.PositiveInfinity;
        Router? smallest = null;
        foreach (var router in _routers)
        {
            var loadLevel = router.NumberRegistered / router.Capacity;
            if (loadLevel < smallestLoad)
            {
                smallestLoad = loadLevel;
                smallest = router;
            }
        }

        if (smallest != null)
        {
            _logger.Information($"Sending Router Assignment Request, {smallest.Endpoint}");
            smallest.NumberRegistered++;
            var packet = _balancerPacketFactory.BuildRouterAssignmentPacket(_buffer, smallest.Endpoint);
            _socket.SendTo(packet, SocketFlags.None, receivedEndPoint);
        }
    }

    void HandleRouterInfoPageRequest(Span<byte> packet, SocketAddress fromAddress)
    {
        if (!_balancerPacketFactory.TryParseRouterInfoPageRequest(packet, out byte pageNumber))
        {
            return;
        }
        var span = _buffer.AsSpan();
        int written = _balancerPacketFactory.BuildRouterInfoPageHeader(span, pageNumber);
        var startIndex = (pageNumber - 1) * 200;
        for (int index = startIndex; index < startIndex + 200; index++)
        {
            var router = _routers[index];
            if (router.IsUsed)
            {
                _logger.Debug($"Sending router info for router {index}");
                written += _balancerPacketFactory.WriteRouterInfoPageEntry(span.Slice(written), router.Id, router.Endpoint);
            }
        }
        _socket.SendTo(span.Slice(0, written), SocketFlags.None, fromAddress);
    }

    Router? FindNextUnusedRouter()
    {
        for (int index = 0; index < _routers.Length; index++)
        {
            var router = _routers[index];
            if (!router.IsUsed)
            {
                return router;
            }
        }
        return null;
    }

    void HandleRegisterRouter(int recieved, SocketAddress receivedEndPoint)
    {
        var router = FindNextUnusedRouter();
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
        //_logger.Debug($"Router registered Id: {router.Id}, Capacity: {router.Capacity}, Endpoint: {router.Endpoint}");

        var registerResponsePacket = _balancerPacketFactory.BuildRegisterRouterResponsePacket(_buffer, router.Id);
        _socket.SendTo(registerResponsePacket, SocketFlags.None, receivedEndPoint);
    }

    public Task RunAsync()
    {
        var taskFactory = new TaskFactory();
        var receiveTask = taskFactory.StartNew(RunReceive, TaskCreationOptions.LongRunning);

        return Task.WhenAny(receiveTask);
    }
}