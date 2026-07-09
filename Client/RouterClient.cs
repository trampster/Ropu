using System.Net;
using System.Net.Sockets;
using Ropu.Logging;
using Ropu.Protocol;

namespace Ropu.Client;

public delegate void MessageHandler(Guid fromClientId, Guid toId, Span<byte> message);

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

    public void SendToClient(Guid fromClientId, Guid toClientId, SocketAddress router, Span<byte> data)
    {
        var packet = _routerPacketFactory.BuildIndividualMessagePacket(Buffer, fromClientId, toClientId, data);
        _socket.SendTo(packet, SocketFlags.None, router);
    }

    public void SendToGroup(Guid fromClientId, Guid groupId, GroupMessageType messageType, Span<byte> data)
    {
        if (RouterAddress == null)
        {
            throw new InvalidOperationException("You must set a router address before calling SendHeartbeat");
        }
        var packet = _routerPacketFactory.BuildGroupMessagePacket(Buffer, fromClientId, groupId, messageType, data);
        _logger.Debug("SendingToGroup");
        _socket.SendTo(packet, SocketFlags.None, RouterAddress);
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
                        case (byte)PacketTypes.ClientHeartbeatResponse:
                            _heartbeatResponseEvent.Set();
                            break;
                        case (byte)PacketTypes.UnknownRecipient:
                            HandleUnknownRecipient(_receiveBuffer.AsSpan(0, received));
                            break;
                        case (byte)PacketTypes.IndividualMessage:
                            HandleIndividualMessage(_receiveBuffer.AsSpan(0, received));
                            break;
                        case (byte)PacketTypes.GroupMessage:
                            HandleGroupMessage(_receiveBuffer.AsSpan(0, received));
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
                _logger.Warning($"Exception occurred in RunReceive {exception.ToString()}");
                throw;
            }
        }
    }

    MessageHandler? _individualMessageHandler;

    public void SetIndividualMessageHandler(MessageHandler? handler)
    {
        _individualMessageHandler = handler;
    }

    MessageHandler? _groupMessageHandler;

    public void SetGroupMessageHandler(MessageHandler? handler)
    {
        _groupMessageHandler = handler;
    }

    void HandleIndividualMessage(Span<byte> packet)
    {
        if (!_routerPacketFactory.TryParseIndividualMessagePacket(packet, out Guid fromClientId, out Guid toClientId, out Span<byte> payload))
        {
            _logger.Warning("Could not parse individual message");
            return;
        }
        _individualMessageHandler?.Invoke(fromClientId, toClientId, payload);
    }

    void HandleGroupMessage(Span<byte> packet)
    {
        if (!_routerPacketFactory.TryParseGroupMessagePacket(packet, out Guid fromClientId, out Guid groupId, out GroupMessageType groupMessageType, out Span<byte> payload))
        {
            _logger.Warning("Could not parse group message");
            return;
        }
        _logger.Debug("Received group message");
        _groupMessageHandler?.Invoke(fromClientId, groupId, payload);
    }

    public event EventHandler<Guid>? UnknownRecipient;

    void HandleUnknownRecipient(Span<byte> packet)
    {
        if (!_routerPacketFactory.TryParseUnknownRecipientPacket(packet, out Guid clientId))
        {
            _logger.Warning("Failed to parse Unknown Recipient packet");
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
        _logger.Debug("Sending SubscribeGroups");
        _socket.SendTo(packet, SocketFlags.None, RouterAddress);
    }

    public event EventHandler? GroupSubscribeResponse;

    void HandleSubscribeGroupsResponse(Span<byte> span)
    {
        GroupSubscribeResponse?.Invoke(this, EventArgs.Empty);
    }

    public void SendGroupMessage(Guid fromClientId, Guid groupId, GroupMessageType groupMessageType, Span<byte> payload)
    {
        if (RouterAddress == null)
        {
            throw new InvalidOperationException("You must set a router address before calling SubscribeGroups");
        }
        var packet = _routerPacketFactory.BuildGroupMessagePacket(Buffer, fromClientId, groupId, groupMessageType, payload);
        _socket.SendTo(packet, SocketFlags.None, RouterAddress);
    }
}