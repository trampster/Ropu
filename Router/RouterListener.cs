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

public class RouterListener
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

    public void PostReceive()
    {
        var expiryCheckTime = _lastExpiryCheck + TimeSpan.FromSeconds(30);
        if (expiryCheckTime < DateTime.UtcNow)
        {
            CheckClientExpiries();
            _lastExpiryCheck = DateTime.UtcNow;
        }
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
        //TODO: resend group subscriptions to all distributors

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
}