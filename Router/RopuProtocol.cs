using System.Net;
using System.Net.Sockets;
using Ropu.Protocol;
using Ropu.Logging;
using Ropu.Router;

namespace Router.Router;

public class RopuProtocol
{
    readonly BalancerClient _balancerClient;
    readonly RouterListener _routerClient;
    readonly ILogger _logger;
    byte[] _receiveBuffer = new byte[1024];
    readonly RopuSocket _socket;

    public RopuProtocol(
        RopuSocket socket,
        BalancerClient balancerClient,
        RouterListener routerClient,
        ILogger logger)
    {
        _balancerClient = balancerClient;
        _routerClient = routerClient;
        _logger = logger.ForContext(nameof(RouterListener));
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
            if (received != 0)
            {
                switch (_receiveBuffer[0])
                {
                    // Router Packets
                    case (byte)PacketTypes.RegisterClient:
                        _routerClient.HandleRegisterClientPacket(_receiveBuffer.AsSpan(0, received), socketAddress);
                        break;
                    case (byte)PacketTypes.ClientHeartbeat:
                        _routerClient.HandleHeartbeatPacket(socketAddress);
                        break;
                    case (byte)PacketTypes.IndividualMessage:
                        _routerClient.HandleIndivdiualMessage(_receiveBuffer.AsSpan(0, received), socketAddress);
                        break;
                    case (byte)PacketTypes.SubscribeGroupsRequest:
                        _routerClient.HandleSubscribeGroupsRequest(_receiveBuffer.AsSpan(0, received), socketAddress);
                        break;
                    case (byte)PacketTypes.RegisterRouterResponse:
                        _balancerClient.HandleRegisterRouterResponse(_receiveBuffer.AsSpan(0, received));
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
                }
            }

            _routerClient.PostReceive();
        }
    }
}