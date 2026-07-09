using System.Net;
using System.Net.Sockets;
using Ropu.Logging;
using Ropu.Protocol;

namespace Ropu.Distributor;

public class Group
{
    public SocketAddressList Subscribers
    {
        get;
    } = new SocketAddressList(10);

    public DateTime StreamExpiry
    {
        get;
        set;
    }

    public ushort StreamNumber
    {
        get;
        set;
    } = 0;

    public bool IsStreaming
    {
        get;
        set;
    }

    public bool IsStreamExpired => IsStreaming && StreamExpiry < DateTime.UtcNow;

    public void ExpireStream()
    {
        StreamNumber = 0;
        IsStreaming = false;
        StreamExpiry = DateTime.MinValue;
    }
}

public class GroupSubscriptions
{
    readonly SocketAddress _routerAddress;

    public GroupSubscriptions(SocketAddress routerAddress)
    {
        _routerAddress = routerAddress;
    }

    public SocketAddress RouterAddress => _routerAddress;

    public List<Guid> Groups
    {
        get;
    } = new();

    /// <summary>
    /// If the router resubscribes is set to true,
    /// At every expiry check is set to false,
    /// If it isn't true at the next expiry check 
    /// it means it didn't resubscribe and so is removed.
    /// </summary>
    public bool Current
    {
        get;
        set;
    } = true;
}

public class RouterProtocolHandler
{
    readonly RouterPacketFactory _routerPacketFactory;
    readonly RopuSocket _socket;
    readonly ushort _capacity;
    readonly ILogger _logger;

    int _spareCapacity;
    readonly byte[] _sendBuffer = new byte[1024];

    public RouterProtocolHandler(
        RopuSocket socket,
        ushort capacity,
        ILogger logger)
    {
        _socket = socket;
        _capacity = capacity;
        _spareCapacity = capacity;
        _logger = logger.ForContext(nameof(RouterProtocolHandler));
        _routerPacketFactory = new RouterPacketFactory();

        _streamExpiryCheck = DateTime.Now + StreamTimeout;
    }

    Guid[] _groupsBuffer = new Guid[10000];
    readonly Dictionary<Guid, Group> _groups = new();
    readonly Dictionary<SocketAddress, GroupSubscriptions> _routersGroups = new(new SocketAddressComparer());
    readonly SocketAddressList _routers = new(2000);

    public void HandleSubscribeGroupsRequest(Span<byte> packet, SocketAddress from)
    {
        if (!_routerPacketFactory.TryParseSubscribeGroupsRequest(packet, _groupsBuffer, out Span<Guid> groups))
        {
            _logger.Warning("Failed to parse subscribe group request packet");
            return;
        }

        // remove any old groups for this router
        if (_routersGroups.TryGetValue(from, out GroupSubscriptions? storedGroups))
        {
            foreach (var groupId in storedGroups.Groups)
            {
                if (_groups.TryGetValue(groupId, out Group? group))
                {
                    group.Subscribers.Remove(from);
                }
            }
            storedGroups.Groups.Clear();
        }
        else
        {
            _routers.Add(from);
            var capacityPacket = _routerPacketFactory.BuildDistributorCapacityPacket(_sendBuffer, (ushort)_spareCapacity);
            _socket.SendTo(capacityPacket.Span, from);
        }

        // add new groups
        if (storedGroups == null)
        {
            //need to take a copy because from changes which each packet
            var socketAddress = new SocketAddress(AddressFamily.InterNetwork);

            storedGroups = new(socketAddress);

            socketAddress.CopyFrom(from);
            _routersGroups[socketAddress] = storedGroups;
        }
        storedGroups.Groups.AddRange(groups);

        storedGroups.Current = true;

        foreach (var groupId in groups)
        {
            if (!_groups.TryGetValue(groupId, out Group? group))
            {
                group = new Group();
                _groups[groupId] = group;
            }

            group.Subscribers.Add(from);
        }

        var response = _routerPacketFactory.BuildSubscribeGroupsResponse(_sendBuffer);
        _socket.SendTo(response, from);
    }

    List<(Guid groupId, DateTime expiry, ushort number)> _streams = new();

    public void HandleGroupMessage(Span<byte> packet, SocketAddress from)
    {
        if (!_routerPacketFactory.TryParseGroupMessagePacket(packet, out Guid _, out Guid groupId, out GroupMessageType groupMessageType, out Span<byte> payload))
        {
            _logger.Warning("Failed to parse group message");
            return;
        }

        if (!_groups.TryGetValue(groupId, out Group? group))
        {
            _logger.Warning("Received group message for unknown group");
            return;
        }

        if (groupMessageType == GroupMessageType.Stream)
        {
            int oldSpaceCapacity = _spareCapacity;

            if (group.IsStreaming)
            {
                // Number of recipients could have changed since last packet update
                // the space capacity
                _spareCapacity += group.Subscribers.Length - group.StreamNumber;
            }
            else
            {
                if (_spareCapacity - group.Subscribers.Length < 0)
                {
                    // Not enough space capacity to send to this group
                    var failurePacket = _routerPacketFactory.BuildGroupMessageFailureResponse(_sendBuffer, groupId, GroupPacketFailureReason.Busy);
                    _socket.SendTo(failurePacket, from);
                    return;
                }
                _spareCapacity -= group.Subscribers.Length;
            }
            group.IsStreaming = true;
            group.StreamExpiry = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            group.StreamNumber = (ushort)group.Subscribers.Length;
        }

        // forward group message to routers
        packet.CopyTo(_sendBuffer.AsSpan(0, packet.Length));
        _socket.SendBulk(_sendBuffer.AsMemory(0, packet.Length), group.Subscribers.AsMemory(), from);
    }

    int _capacityBeforePacket = 0;
    DateTime _streamExpiryCheck = DateTime.MaxValue;
    readonly TimeSpan StreamTimeout = TimeSpan.FromSeconds(2);
    readonly TimeSpan SubscriptionTimeout = TimeSpan.FromMinutes(10);
    DateTime _lastSubscriptionExpiryCheck = DateTime.MinValue;


    readonly List<SocketAddress> _toRemove = new();
    public void OnBeforePacket()
    {
        _capacityBeforePacket = _spareCapacity;
        if (_streamExpiryCheck + StreamTimeout < DateTime.UtcNow)
        {
            foreach (var subscription in _routersGroups.Values)
            {
                if (!subscription.Current)
                {
                    //remove
                    _toRemove.Add(subscription.RouterAddress);
                    foreach (var group in subscription.Groups)
                    {
                        if (_groups.TryGetValue(group, out Group? groupSubscribers))
                        {
                            groupSubscribers.Subscribers.Remove(subscription.RouterAddress);
                            if (groupSubscribers.Subscribers.Length == 0)
                            {
                                _groups.Remove(group);
                            }
                        }
                    }
                    _routers.Remove(subscription.RouterAddress);
                    continue;
                }
                // set to false, if it still false on the next check it means
                // the it didn't resubscribe
                subscription.Current = false;
            }
            foreach (var toRemove in _toRemove)
            {
                _routersGroups.Remove(toRemove);
            }
            _lastSubscriptionExpiryCheck = DateTime.UtcNow;
        }
    }

    public void OnAfterPacket()
    {
        if (_streamExpiryCheck + StreamTimeout < DateTime.UtcNow)
        {
            foreach (var group in _groups.Values)
            {
                if (group.IsStreamExpired)
                {
                    _spareCapacity -= group.StreamNumber;
                    group.ExpireStream();
                }
            }
        }
        _streamExpiryCheck = DateTime.UtcNow;

        if (_capacityBeforePacket != _spareCapacity)
        {
            // inform routers of spare capacity change
            var packet = _routerPacketFactory.BuildDistributorCapacityPacket(_sendBuffer, (ushort)_spareCapacity);
            _socket.SendBulk(_sendBuffer.AsMemory(0, packet.Length), _routers.AsMemory(), null);
        }
    }
}