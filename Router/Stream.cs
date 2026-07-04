using System.Net;
using Ropu.Protocol;

namespace Ropu.Router;

public class Stream
{
    readonly SocketAddress _distributorAddress = new(System.Net.Sockets.AddressFamily.InterNetwork);
    DateTime _lastUsed = DateTime.MinValue;

    public SocketAddress DistributorAddress
    {
        get => _distributorAddress;
        set => _distributorAddress.CopyFrom(value);
    }

    public void Refresh() => _lastUsed = DateTime.UtcNow;

    public bool IsExpired() => _lastUsed + TimeSpan.FromSeconds(5) < DateTime.UtcNow;

    public Guid GroupId { get; set; }
}