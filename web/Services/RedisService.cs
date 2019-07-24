using StackExchange.Redis;

namespace Ropu.Web.Services
{
    public class RedisService
    {
        readonly ConnectionMultiplexer _connectionMultiplexer;

        public RedisService(ConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public IDatabase GetDatabase()
        {
            return _connectionMultiplexer.GetDatabase();
        }

        public long CalculateStringScore(string item)
        {
            item = item.ToLowerInvariant();
            int length = item.Length;
            long score = 
                ((length < 1) ? 0 : ((long)item[0] << 48)) + 
                ((length < 2) ? 0 : ((long)item[1] << 32)) + 
                ((length < 3) ? 0 : ((long)item[2] << 16)) + 
                ((length < 4) ? 0 : ((long)item[3]));
            return score;
        }
    }
}