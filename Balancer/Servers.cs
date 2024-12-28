using System.Diagnostics.CodeAnalysis;
using System.Net;
using Ropu.Logging;

namespace Ropu.Balancer;

public class Servers
{
    readonly string _name;
    readonly Server[] _servers;
    readonly ILogger _logger;

    public Servers(string name, ILogger logger)
    {
        _name = name;
        _logger = logger;
        _servers = new Server[2000];
        for (int index = 0; index < _servers.Length; index++)
        {
            _servers[index] = new Server()
            {
                Id = (ushort)index
            };
        }
    }

    public int Count()
    {
        int total = 0;
        for (int index = 0; index < _servers.Length; index++)
        {
            if (_servers[index].IsUsed)
            {
                total++;
            }
        }
        return total;
    }

    public Server? FindNextUnused()
    {
        for (int index = 0; index < _servers.Length; index++)
        {
            var server = _servers[index];
            if (!server.IsUsed)
            {
                return server;
            }
        }
        return null;
    }

    public void CheckLastSeen()
    {
        for (int index = 0; index < _servers.Length; index++)
        {
            var router = _servers[index];
            if (router.IsUsed && !router.Seen)
            {
                _logger.Information($"{_name} {router.Id} timed out removing");
                router.IsUsed = false;
            }
            else
            {
                router.Seen = false;
            }
        }
    }

    public bool TryGet(int id, [NotNullWhen(true)] out Server? router)
    {
        if (id >= _servers.Length)
        {
            router = null;
            return false;
        }
        var routerAtIndex = _servers[id];
        if (routerAtIndex.IsUsed)
        {
            router = routerAtIndex;
            return true;
        }
        router = null;
        return false;
    }

    public Server[] ServersArray => _servers;
}