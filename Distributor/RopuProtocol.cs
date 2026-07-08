using System.Net;
using System.Net.Sockets;
using Ropu.Protocol;
using Ropu.Logging;

namespace Ropu.Distributor;

public class RopuProtocol
{
    readonly BalancerClient _balancerClient;
    readonly RouterProtocolHandler _routerClient;
    readonly ILogger _logger;
    byte[] _receiveBuffer = new byte[1024];
    readonly RopuSocket _socket;

    public RopuProtocol(
        RopuSocket socket,
        BalancerClient balancerClient,
        RouterProtocolHandler routerClient,
        ILogger logger)
    {
        _balancerClient = balancerClient;
        _routerClient = routerClient;
        _logger = logger.ForContext(nameof(RopuProtocol));
        _socket = socket;
    }

    public Task RunReceiveAsync(CancellationToken cancellationToken)
    {
        var taskFactory = new TaskFactory();
        return taskFactory.StartNew(() => RunReceive(cancellationToken), TaskCreationOptions.LongRunning);
    }

    public void RunReceive(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var socketAddress = new SocketAddress(AddressFamily.InterNetworkV6);
            var received = _socket.ReceiveFrom(socketAddress, _receiveBuffer);
            if (received == 0)
            {
                continue;
            }
            _routerClient.OnBeforePacket();
            switch (_receiveBuffer[0])
            {
                // Balancer Packets
                case (byte)PacketTypes.RegisterDistributorResponse:
                    _balancerClient.HandleRegisterDistributorResponse(_receiveBuffer.AsSpan(0, received));
                    break;
                case (byte)PacketTypes.BalancerHeartbeatResponse:
                    _balancerClient.HandleBalancerHeartbeatResponse();
                    break;
                case (byte)PacketTypes.DistributorList:
                    _balancerClient.HandleDistributorList(_receiveBuffer.AsSpan(0, received));
                    break;
                default:
                    _logger.Warning($"Received unknown packet type: {_receiveBuffer[0]}");
                    break;
                // Router Packets
                case (byte)PacketTypes.SubscribeGroupsRequest:
                    _routerClient.HandleSubscribeGroupsRequest(_receiveBuffer.AsSpan(0, received), socketAddress);
                    break;
                case (byte)PacketTypes.GroupMessage:
                    _routerClient.HandleGroupMessage(_receiveBuffer.AsSpan(0, received), socketAddress);
                    break;
            }
            _routerClient.OnAfterPacket();
        }
    }
}