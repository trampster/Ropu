using System.Net;
using System.Net.Sockets;
using Ropu.Protocol;
using Ropu.Logging;

namespace Ropu.Router;

public class Client
{
    public Client()
    {
        AddedDate = DateTime.UtcNow;
    }

    public Guid ClientId
    {
        get;
        set;
    }

    public SocketAddress Address
    {
        get;
        set;
    } = new SocketAddress(AddressFamily.InterNetwork);


    public List<Guid> Groups
    {
        get;
    } = [];

    public DateTime AddedDate
    {
        get;
    }

    public bool IsExpired()
    {
        var expiryTime = AddedDate + TimeSpan.FromSeconds(60);
        return expiryTime < DateTime.UtcNow;
    }
}

public class Group
{
    public SocketAddressList SocketAddressList
    {
        get;
    } = new(10);
}

public class RouterListener : IDistributorsListener
{
    readonly RopuSocket _socket;
    readonly DistributorsManager _distributorsManager;
    readonly ILogger _logger;
    readonly RouterPacketFactory _routerPacketFactory;

    readonly Dictionary<Guid, Client> _addressBook = [];
    readonly Dictionary<SocketAddress, Client> _addresses = [];

    readonly byte[] _sendBuffer = new byte[65535];

    Guid[] _groupsBuffer = new Guid[2000];

    // value is the number of clients subscribed
    readonly Dictionary<Guid, Group> _groupInfos = new();

    /// <summary>
    /// Used for sending group subscriptions to distributors
    /// </summary>
    readonly GuidList _groupsList = new GuidList(2000);

    public RouterListener(
        RopuSocket socket,
        RouterPacketFactory routerPacketFactory,
        DistributorsManager distributorsManager,
        ILogger logger)
    {
        _routerPacketFactory = routerPacketFactory;
        _distributorsManager = distributorsManager;
        _logger = logger.ForContext(nameof(RouterListener));
        _socket = socket;
    }

    public IReadOnlyDictionary<Guid, Client> Clients => _addressBook;

    public SocketAddress? RouterAddress
    {
        get;
        set;
    }

    DateTime _lastExpiryCheck = DateTime.MinValue;
    DateTime _lastSubscriptionCheck = DateTime.MinValue;
    DateTime _lastSubscriptionRenewal = DateTime.MinValue;

    DateTime _lastStreamCheckTime = DateTime.UtcNow;

    public void BeforeReceive()
    {
        var expiryCheckTime = _lastStreamCheckTime + TimeSpan.FromSeconds(1);
        if (expiryCheckTime < DateTime.UtcNow)
        {
            var streams = _stream.AsSpan();
            for (int streamIndex = _stream.Length - 1; streamIndex >= 0; streamIndex--)
            {
                var stream = streams[streamIndex];
                if (stream.IsExpired())
                {
                    _streamLookup.Remove(stream.GroupId);
                    _stream.RemoveAt(streamIndex);
                }
            }
        }
        _lastStreamCheckTime += TimeSpan.FromSeconds(5);
    }

    public void PostReceive()
    {
        var expiryCheckTime = _lastExpiryCheck + TimeSpan.FromSeconds(30);
        if (expiryCheckTime < DateTime.UtcNow)
        {
            CheckClientExpiries();
            _lastExpiryCheck = DateTime.UtcNow;
        }

        var subscriptionCheckTime = _lastSubscriptionCheck + TimeSpan.FromSeconds(5);
        if (subscriptionCheckTime < DateTime.UtcNow)
        {
            CheckDistributorSubscriptions();
            _lastSubscriptionCheck = DateTime.UtcNow;

            // we put it here so that we only check it every 5 seconds instead of on every packet
            var subscriptionRenewalTime = _lastSubscriptionRenewal + TimeSpan.FromMinutes(7);
            if (subscriptionRenewalTime < DateTime.UtcNow)
            {
                SendGroupSubscriptionsToDistributors();
                _lastSubscriptionRenewal = DateTime.UtcNow;
            }

            var streams = _stream.AsSpan();
            for (int streamIndex = _stream.Length - 1; streamIndex >= 0; streamIndex--)
            {
                var stream = streams[streamIndex];
                if (stream.IsExpired())
                {
                    _streamLookup.Remove(stream.GroupId);
                    _stream.RemoveAt(streamIndex);
                }
            }
        }
    }

    void CheckDistributorSubscriptions()
    {
        var subscribeGroupsPacket = _routerPacketFactory.BuildSubscribeGroupsRequest(_sendBuffer, _groupsList.AsSpan());

        _socket.SendBulk(_sendBuffer.AsMemory(subscribeGroupsPacket.Length), _distributorsManager.NotSubscribedDistributors, null);
    }

    void CheckClientExpiries()
    {
        foreach (var client in _addressBook.Values)
        {
            if (client.IsExpired())
            {
                _addressBook.Remove(client.ClientId);
                _addresses.Remove(client.Address);

                foreach (var groupId in client.Groups)
                {
                    var group = _groupInfos[groupId];
                    group.SocketAddressList.Remove(client.Address);
                    if (group.SocketAddressList.AsSpan().Length == 0)
                    {
                        // no longer need to be subscribed to this group
                        _groupInfos.Remove(groupId);
                        _groupsList.Remove(groupId);

                        SendGroupSubscriptionsToDistributors();
                    }
                }
            }
        }
    }

    void SendGroupSubscriptionsToDistributors()
    {
        var subscribeGroupsPacket = _routerPacketFactory.BuildSubscribeGroupsRequest(_sendBuffer, _groupsList.AsSpan());

        _distributorsManager.ClearSubsciptions();
        _distributorsManager.OnSubscriptionSent(_distributorsManager.Distributors);
        _socket.SendBulk(_sendBuffer.AsMemory(subscribeGroupsPacket.Length), _distributorsManager.DistributorsMemory, null);
    }

    public void HandleSubscribeGroupsRequest(Span<byte> packet, SocketAddress socketAddress)
    {
        if (!_addresses.TryGetValue(socketAddress, out Client? client))
        {
            //don't know who this is
            _logger.Warning("Received Subscribe Group Request from unknown client");
            return;
        }
        _routerPacketFactory.TryParseSubscribeGroupsRequest(packet, _groupsBuffer, out Span<Guid> groups);
        client.Groups.Clear();
        bool didGroupListChange = false;
        foreach (var groupId in groups)
        {
            client.Groups.Add(groupId);
            if (_groupInfos.TryGetValue(groupId, out Group? group))
            {
                // existing group
                group.SocketAddressList.Add(socketAddress);
                continue;
            }
            // it's new
            var newGroup = new Group();
            newGroup.SocketAddressList.Add(socketAddress);
            _groupInfos[groupId] = newGroup;
            _groupsList.Add(groupId);
            didGroupListChange = true;
        }

        if (didGroupListChange)
        {
            SendGroupSubscriptionsToDistributors();
        }
    }

    public void HandleIndivdiualMessage(Span<byte> packet, SocketAddress socketAddress)
    {
        _logger.Information("Received Indivdiual Message");
        if (!_routerPacketFactory.TryParseUnitIdFromIndividualMessagePacket(packet, out Guid unitId))
        {
            _logger.Warning("Could not parse Individual Message2");
            return;
        }

        if (!_addressBook.TryGetValue(unitId, out Client? client))
        {
            _logger.Warning($"Could not forward Individual Message because unit {unitId.ToString()} is not registered");
            var unknownRecipientPacket = _routerPacketFactory.BuildUnknownRecipientPacket(_sendBuffer, unitId);
            _socket.SendTo(unknownRecipientPacket, socketAddress);
            return;
        }

        _socket.SendTo(packet, client.Address);
    }

    public void HandleRegisterClientPacket(Span<byte> packet, SocketAddress socketAddress)
    {
        if (!_routerPacketFactory.TryParseRegisterClientPacket(packet, out Guid clientId))
        {
            _logger.Warning($"Failed to parse Register Client packet");
            return;
        }
        var client = new Client();
        client.ClientId = clientId;
        client.Address.CopyFrom(socketAddress);
        _addressBook[clientId] = client;
        _addresses.Add(client.Address, client);
        _logger.Debug($"Client registered {clientId.ToString()}");
        var response = _routerPacketFactory.BuildRegisterClientResponse(_sendBuffer);
        _socket.SendTo(response, socketAddress);
    }

    public void HandleHeartbeatPacket(SocketAddress socketAddress)
    {
        if (_addresses.ContainsKey(socketAddress))
        {
            _socket.SendTo(RouterPacketFactory.HeartbeatResponsePacket, socketAddress);
        }
    }

    public void OnAdded(Span<SocketAddress> distributors)
    {
        var subscribeGroupsPacket = _routerPacketFactory.BuildSubscribeGroupsRequest(_sendBuffer, _groupsList.AsSpan());
        _distributorsManager.OnSubscriptionSent(distributors);

        foreach (var distributor in distributors)
        {
            _socket.SendTo(subscribeGroupsPacket, distributor);
        }
    }

    public void HandleGroupMessage(Span<byte> packet, SocketAddress socketAddress)
    {
        if (_routerPacketFactory.TryParseGroupMessagePacket(packet, out Guid groupId, out GroupMessageType groupMessageType, out Span<byte> payload))
        {
            _logger.Warning($"Failed to parse Group Message Packet");
            return;
        }
        if (groupMessageType == GroupMessageType.OneOff)
        {
            var distributorAddress = _distributorsManager.GetFreeDistributor();
            if (distributorAddress == null)
            {
                _logger.Warning("No distributors available to handle group message");
                return;
            }
            _socket.SendTo(packet, distributorAddress);
            return;
        }
        //stream message
        if (_streamLookup.TryGetValue(groupId, out Stream? stream))
        {
            // existing stream
            _socket.SendTo(packet, stream.DistributorAddress);
            return;
        }

        // it's a new stream so choose a distributor
        var distributor = _distributorsManager.GetFreeDistributor();
        if (distributor == null)
        {
            _logger.Warning($"No free distributor for streaming to group");
            return;
        }
        var newStream = _stream.Add();
        newStream.Refresh();
        newStream.DistributorAddress = distributor;
        newStream.GroupId = groupId;

        _streamLookup.Add(groupId, newStream);
        _socket.SendTo(packet, distributor);
    }

    readonly UnorderedList<Stream> _stream = new(100);
    readonly Dictionary<Guid, Stream> _streamLookup = new();

    public void HandleDistributorCapacity(Span<byte> packet, SocketAddress from)
    {
        if (!_routerPacketFactory.TryParseDistributorCapacityPacket(packet, out ushort capacity))
        {
            _logger.Warning("Failed to parse Distributor Capacity packet");
            return;
        }
        _distributorsManager.UpdateCapacity(from, capacity);
    }
}