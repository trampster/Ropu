using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Ropu.Logging;
using Ropu.RouterProtocol;

namespace Ropu.Client;

public class RouterClient
{
    readonly Socket _socket;
    readonly ILogger _logger;
    readonly RouterPacketFactory _routerPacketFactory;

    [ThreadStatic]
    static byte[]? _buffer;

    public RouterClient(
        RouterPacketFactory routerPacketFactory,
        ILogger logger)
    {
        _routerPacketFactory = routerPacketFactory;
        _logger = logger.ForContext(nameof(RouterClient));
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var endpoint = new IPEndPoint(IPAddress.Any, 0);
        _socket.Bind(endpoint);
    }

    public SocketAddress RouterAddress
    {
        get;
        set;
    }

    byte[] Buffer
    {
        get
        {
            if (_buffer == null)
            {
                _buffer = new byte[1024];
            }
            return _buffer;
        }
    }

    public bool Register(uint clientId)
    {
        var buffer = Buffer;
        var packet = _routerPacketFactory.BuildRegisterClientPacket(buffer, clientId);
        _registerResponseEvent.Reset();
        _socket.SendTo(packet, SocketFlags.None, RouterAddress);
        return _registerResponseEvent.WaitOne(2000);
    }

    public bool SendHeartbeat()
    {
        _heartbeatResponseEvent.Reset();
        _socket.SendTo(RouterPacketFactory.HeartbeatPacket, SocketFlags.None, RouterAddress);
        return _heartbeatResponseEvent.WaitOne(2000);
    }

    public Task RunReceiveAsync(CancellationToken cancellationToken)
    {
        var taskFactory = new TaskFactory();
        return taskFactory.StartNew(() => RunReceive(cancellationToken), TaskCreationOptions.LongRunning);
    }

    byte[] _receiveBuffer = new byte[1024];

    readonly ManualResetEvent _registerResponseEvent = new(false);
    readonly ManualResetEvent _heartbeatResponseEvent = new(false);

    public void RunReceive(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var buffer = Buffer;
            var socketAddress = new SocketAddress(AddressFamily.InterNetworkV6);
            var received = _socket.ReceiveFrom(buffer, SocketFlags.None, socketAddress);
            if (received != 0)
            {
                switch (_receiveBuffer[0])
                {
                    case (byte)RouterPacketType.RegisterClientResponse:
                        _registerResponseEvent.Set();
                        break;
                    case (byte)RouterPacketType.HeartbeatResponse:
                        _heartbeatResponseEvent.Set();
                        break;
                    default:
                        _logger.Warning($"Received unknown packet type: {_receiveBuffer[0]}");
                        break;
                }
            }
        }
    }
}