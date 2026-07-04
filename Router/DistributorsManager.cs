using System.Net;
using System.Net.Sockets;
using Ropu.Logging;
using Ropu.Protocol;

namespace Ropu.Router;

public class Distributor
{
    readonly SocketAddress _address = new(System.Net.Sockets.AddressFamily.InterNetwork);

    public SocketAddress Address
    {
        get => _address;
        set => _address.CopyFrom(value);
    }

    public int Capacity
    {
        get;
        set;
    }

    public bool InNewList
    {
        get;
        set;
    }
}

public class DistributorsManager
{
    readonly ILogger _logger;
    readonly SocketAddressList _distributorSockets = new(2000);

    // These are distributors we have sent subscriptions to but havn't recieved responses
    readonly SocketAddressList _notSubscribedDistributors = new(2000);

    readonly Dictionary<SocketAddress, Distributor> _lookup = new();
    readonly UnorderedList<Distributor> _distributors = new(2000);

    public DistributorsManager(ILogger logger)
    {
        _logger = logger.ForContext(nameof(DistributorsManager));
    }

    public Span<SocketAddress> Distributors => _distributorSockets.AsSpan();
    public Memory<SocketAddress> DistributorsMemory => _distributorSockets.AsMemory();

    public event EventHandler? DistributorsChanged;

    public IDistributorsListener? DistributorsListener { get; set; }

    public void ReplaceList(Span<SocketAddress> addresses)
    {
        _distributorSockets.Clear();
        foreach (var distributorAddress in addresses)
        {
            _distributorSockets.Add(distributorAddress);
        }

        // reset all
        var distributors = _distributors.AsSpan();
        foreach (var distributor in distributors)
        {
            distributor.InNewList = false;
        }

        // mark onces are are in the new list
        foreach (var address in _distributorSockets.AsSpan()) // use _distributorSockets as this is a copy
        {
            if (_lookup.TryGetValue(address, out Distributor? distributor))
            {
                //already exists
                distributor.InNewList = true;
                continue;
            }
            //is new add it
            var newDistributor = _distributors.Add();
            newDistributor.Address = address;
            newDistributor.InNewList = true;
            newDistributor.Capacity = 0; //until we get a Distributor Capacity packet best to assume it's busy
            _lookup.Add(address, newDistributor);
        }

        // everything unmarked is not longer in the list
        for (int index = distributors.Length - 1; index >= 0; index--)
        {
            var distributor = distributors[index];
            if (!distributor.InNewList)
            {
                _lookup.Remove(distributor.Address);
                _distributors.Remove(distributor);
            }
        }
        DistributorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSubsciptions()
    {
        _notSubscribedDistributors.Clear();
    }

    public void OnSubscriptionSent(Span<SocketAddress> distributors)
    {
        _notSubscribedDistributors.AddRange(distributors);
    }

    public void OnSubscriptionResponseReceived(SocketAddress distributor)
    {
        _notSubscribedDistributors.Remove(distributor);
    }

    public Memory<SocketAddress> NotSubscribedDistributors => _notSubscribedDistributors.AsMemory();

    public void Add(Span<SocketAddress> addresses)
    {

        foreach (var address in addresses) // use _distributorSockets as this is a copy
        {
            var addressCopy = _distributorSockets.Add(address);

            var newDistributor = _distributors.Add();
            newDistributor.Address = addressCopy;
            newDistributor.InNewList = true;
            newDistributor.Capacity = 0; //until we get a Distributor Capacity packet best to assume it's busy
            _lookup.Add(addressCopy, newDistributor);
        }

        DistributorsListener?.OnAdded(addresses);
        DistributorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Span<SocketAddress> addresses)
    {
        _distributorSockets.RemoveRange(addresses);

        foreach (var address in addresses)
        {
            if (_lookup.TryGetValue(address, out Distributor? distributor))
            {
                _distributors.Remove(distributor);
                _lookup.Remove(address);
            }
        }
        DistributorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateCapacity(SocketAddress from, ushort capacity)
    {
        if (!_lookup.TryGetValue(from, out Distributor? distributor))
        {
            _logger.Warning("Tried to udpate capcity for unknown distributor");
            return;
        }
        distributor.Capacity = capacity;
    }

    public SocketAddress? GetFreeDistributor()
    {
        var distributors = _distributors.AsSpan();
        if (distributors.Length == 0)
        {
            return null; // no distributors at all :(
        }
        Distributor candidateDistributor = distributors[0];
        foreach (var distributor in _distributors.AsSpan())
        {
            if (distributor.Capacity > candidateDistributor.Capacity)
            {
                candidateDistributor = distributor;
                continue;
            }
            if (distributor.Capacity == candidateDistributor.Capacity)
            {
                // flip a coin
                if (Random.Shared.Next(2) == 0)
                {
                    continue;
                }
                candidateDistributor = distributor;
            }
        }
        if (candidateDistributor.Capacity == 0)
        {
            _logger.Warning("All distributors are busy");
            return null;
        }
        return candidateDistributor.Address;
    }
}