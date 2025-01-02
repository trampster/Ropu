using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.Marshalling;
using Ropu.Logging;
using Ropu.RouterProtocol;

namespace Ropu.Client;

public class RouterListener : IDisposable
{
    readonly Socket _socket;
    readonly ILogger _logger;
    readonly RouterPacketFactory _routerPacketFactory;

    readonly ConcurrentDictionary<uint, SocketAddress> _addressBook = [];
    readonly HashSet<SocketAddress> _addresses = [];

    [ThreadStatic]
    static byte[]? _buffer;

    public RouterListener(
        ushort port,
        RouterPacketFactory routerPacketFactory,
        ILogger logger)
    {
        _routerPacketFactory = routerPacketFactory;
        _logger = logger.ForContext(nameof(RouterListener));
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        var endpoint = new IPEndPoint(IPAddress.Any, port);
        _socket.Bind(endpoint);
    }

    public IReadOnlyDictionary<uint, SocketAddress> Clients => _addressBook;

    public SocketAddress? RouterAddress
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

    public Task RunReceiveAsync(CancellationToken cancellationToken)
    {
        var taskFactory = new TaskFactory();
        return taskFactory.StartNew(() => RunReceive(cancellationToken), TaskCreationOptions.LongRunning);
    }

    byte[] _receiveBuffer = new byte[1024];

    public void RunReceive(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var socketAddress = new SocketAddress(AddressFamily.InterNetworkV6);
            var received = _socket.ReceiveFrom(_receiveBuffer, SocketFlags.None, socketAddress);
            if (received != 0)
            {
                switch (_receiveBuffer[0])
                {
                    case (byte)RouterPacketType.RegisterClient:
                        HandleRegisterClientPacket(_receiveBuffer.AsSpan(0, received), socketAddress);
                        break;
                    case (byte)RouterPacketType.Heartbeat:
                        HandleHeartbeatPacket(socketAddress);
                        break;
                    case (byte)RouterPacketType.IndividualMessage:
                        HandleIndivdiualMessage(_receiveBuffer.AsSpan(0, received), socketAddress);
                        break;
                    default:
                        _logger.Warning($"Received unknown packet type: {_receiveBuffer[0]}");
                        break;
                }
            }
        }
    }

    void HandleIndivdiualMessage(Span<byte> packet, SocketAddress socketAddress)
    {
        _logger.Information("Received Indivdiual Message");
        if (!_routerPacketFactory.TryParseUnitIdFromIndividualMessagePacket(packet, out uint unitId))
        {
            _logger.Warning("Could not parse Individual Message");
            return;
        }

        if (!_addressBook.TryGetValue(unitId, out SocketAddress? unitAddress))
        {
            _logger.Warning($"Could forward Individual Message because unit {unitId} is not registered");
            var buffer = Buffer;
            var unknownRecipientPacket = _routerPacketFactory.BuildUnknownRecipientPacket(buffer, unitId);
            _socket.SendTo(unknownRecipientPacket, SocketFlags.None, socketAddress);
            return;
        }

        _socket.SendTo(packet, SocketFlags.None, unitAddress);
    }

    void HandleRegisterClientPacket(Span<byte> packet, SocketAddress socketAddress)
    {
        _routerPacketFactory.TryParseRegisterClientPacket(packet, out uint clientId);
        _addressBook[clientId] = socketAddress;
        _addresses.Add(socketAddress);
        var response = _routerPacketFactory.BuildRegisterClientResponse(_receiveBuffer);
        _socket.SendTo(response, SocketFlags.None, socketAddress);
    }

    void HandleHeartbeatPacket(SocketAddress socketAddress)
    {
        if (_addresses.Contains(socketAddress))
        {
            _socket.SendTo(RouterPacketFactory.HeartbeatResponsePacket, SocketFlags.None, socketAddress);
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