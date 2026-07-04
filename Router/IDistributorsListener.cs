using System.Net;

namespace Ropu.Router;

public interface IDistributorsListener
{
    void OnAdded(Span<SocketAddress> distributors);
}