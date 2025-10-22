using System.Diagnostics.CodeAnalysis;
using System.Net;
using Ropu.Logging;

namespace Ropu.Balancer;

public class Servers
{
    readonly string _name;
    readonly Server[] _servers;
    readonly Server[] _idLookup;
    readonly SocketAddress[] _replyAddresses;
    readonly ILogger _logger;

    int _length = 0;

    public Servers(string name, ILogger logger)
    {
        _name = name;
        _logger = logger;
        _servers = new Server[2000];
        _idLookup = new Server[2000];
        _replyAddresses = new SocketAddress[2000];
        for (int index = 0; index < _servers.Length; index++)
        {
            _servers[index] = new Server()
            {
                Id = (ushort)index
            };
            _idLookup[index] = _servers[index];
        }
    }

    public int Count() => _length;

    public Server? CheckExisting(SocketAddress replayAddress)
    {
        foreach (var server in Span)
        {
            if (server.ReplyAddress == replayAddress)
            {
                return server;
            }
        }
        return null;
    }

    public Server? FindNextUnused()
    {
        if (_servers.Length == _length)
        {
            return null;
        }
        var server = _servers[_length];
        return server;
    }

    public void SetUsed(Server server)
    {
        server.IsUsed = true;
        _replyAddresses[_length] = server.ReplyAddress;
        _length++;
    }

    public Span<Server> CheckLastSeen(Server[] serversBuffer)
    {
        int outIndex = 0;
        for (int index = 0; index < _length; index++)
        {
            var router = _servers[index];
            if (router.IsUsed && !router.Seen)
            {
                _logger.Information($"{_name} {router.Id} timed out removing");

                int lastUsedRouterIndex = _length - 1;
                if (lastUsedRouterIndex < 0)
                {
                    break;
                }

                var lastUsedRouter = _servers[lastUsedRouterIndex];
                _servers[index] = lastUsedRouter;
                _servers[lastUsedRouterIndex] = router;

                _replyAddresses[index] = lastUsedRouter.ReplyAddress;


                _length--;
                router.IsUsed = false;
                serversBuffer[outIndex] = router;
                outIndex++;
            }
            else
            {
                router.Seen = false;
            }
        }
        return serversBuffer.AsSpan(0, outIndex);
    }

    public bool TryGet(int id, [NotNullWhen(true)] out Server? router)
    {
        if (id >= _idLookup.Length)
        {
            router = null;
            return false;
        }
        var routerAtIndex = _idLookup[id];
        if (routerAtIndex.IsUsed)
        {
            router = routerAtIndex;
            return true;
        }
        router = null;
        return false;
    }

    public Span<Server> Span => _servers.AsSpan(0, _length);
    public Memory<SocketAddress> ReplyAddresses => _replyAddresses.AsMemory(0, _length);
}