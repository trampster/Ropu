using System.Net;
using System.Net.Sockets;
using Ropu.Logging;
using Ropu.Protocol;

namespace Ropu.Client;

public delegate void IndividualMessageHandler(Span<byte> message);

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

    public bool Register(Guid clientId)
    {
        if (RouterAddress == null)
        {
            throw new InvalidOperationException("You must set a router address before calling Register");
        }
        var buffer = Buffer;
        var packet = _routerPacketFactory.BuildRegisterClientPacket(buffer, clientId);
        _registerResponseEvent.Reset();
        _socket.SendTo(packet, SocketFlags.None, RouterAddress);
        return _registerResponseEvent.WaitOne(2000);
    }

    public bool SendHeartbeat()
    {
        if (RouterAddress == null)
        {
            throw new InvalidOperationException("You must set a router address before calling SendHeartbeat");
        }
        _heartbeatResponseEvent.Reset();
        _socket.SendTo(RouterPacketFactory.HeartbeatPacket, SocketFlags.None, RouterAddress);
        return _heartbeatResponseEvent.WaitOne(2000);
    }

    public void SendToClient(Guid clientId, SocketAddress router, Span<byte> data)
    {
        var packet = _routerPacketFactory.BuildIndividualMessagePacket(Buffer, clientId, data);
        _socket.SendTo(packet, SocketFlags.None, router);
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
        var socketAddress = new SocketAddress(AddressFamily.InterNetworkV6);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var received = _socket.ReceiveFrom(_receiveBuffer, SocketFlags.None, socketAddress);
                if (received != 0)
                {
                    switch (_receiveBuffer[0])
                    {
                        case (byte)PacketTypes.RegisterClientResponse:
                            _logger.Debug("Received register response");
                            _registerResponseEvent.Set();
                            break;
                        case (byte)PacketTypes.ClientHeartbeat:
                            _heartbeatResponseEvent.Set();
                            break;
                        case (byte)PacketTypes.UnknownRecipient:
                            HandleUnknownRecipient(_receiveBuffer.AsSpan(0, received));
                            break;
                        case (byte)PacketTypes.IndividualMessage:
                            HandleIndividualMessage(_receiveBuffer.AsSpan(0, received));
                            break;
                        case (byte)PacketTypes.SubscribeGroupsResponse:
                            HandleSubscribeGroupsResponse(_receiveBuffer.AsSpan(0, received));
                            break;
                        default:
                            _logger.Warning($"Received unknown packet type: {_receiveBuffer[0]}");
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Warning($"Exception occured in RunReceive {exception.ToString()}");
                throw;
            }
        }
    }

    IndividualMessageHandler? _individualMessageHandler;

    public void SetIndividualMessageHandler(IndividualMessageHandler? handler)
    {
        _individualMessageHandler = handler;
    }

    void HandleIndividualMessage(Span<byte> packet)
    {
        if (!_routerPacketFactory.TryParseIndividualMessagePacket(packet, out Guid clientId, out Span<byte> payload))
        {
            _logger.Warning("Could not parse individual message1");
            return;
        }
        _individualMessageHandler?.Invoke(payload);
    }

    public event EventHandler<Guid>? UnknownRecipient;

    void HandleUnknownRecipient(Span<byte> packet)
    {
        if (!_routerPacketFactory.TryParseUnknownRecipientPacket(packet, out Guid clientId))
        {
            _logger.Warning("Failed to parse Unknown Recipient pakcet");
            return;
        }
        UnknownRecipient?.Invoke(this, clientId);
    }

    public void SendSubscribeGroups(Span<Guid> groups)
    {
        if (RouterAddress == null)
        {
            throw new InvalidOperationException("You must set a router address before calling SubscribeGroups");
        }
        var packet = _routerPacketFactory.BuildSubscribeGroupsRequest(Buffer, groups);
        _socket.SendTo(packet, SocketFlags.None, RouterAddress);
    }

    public event EventHandler? GroupSubscribeReponse;

    void HandleSubscribeGroupsResponse(Span<byte> span)
    {
        GroupSubscribeReponse?.Invoke(this, EventArgs.Empty);
    }
}