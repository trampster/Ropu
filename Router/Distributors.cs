using System.Net;
using Ropu.Protocol;

namespace Ropu.Router;

public class DistributorsManager
{
    readonly SocketAddressList _distributors = new(2000);
    public Span<SocketAddress> Distributors => _distributors.AsSpan();

    public event EventHandler? DistributorsChanged;

    public void ReplaceList(Span<SocketAddress> distributors)
    {
        _distributors.Clear();
        _distributors.AddRange(distributors);
        DistributorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Add(Span<SocketAddress> distributors)
    {
        _distributors.AddRange(distributors);
        DistributorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Span<SocketAddress> distributors)
    {
        _distributors.RemoveRange(distributors);
        DistributorsChanged?.Invoke(this, EventArgs.Empty);
    }

}