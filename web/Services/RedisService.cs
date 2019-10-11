using System;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Ropu.Web.Services
{
    public class RedisService
    {
        readonly ConnectionMultiplexer _connectionMultiplexer;
        readonly ILogger _logger;

        public RedisService(ConnectionMultiplexer connectionMultiplexer, ILogger<RedisService> logger)
        {
            _logger = logger;
            _connectionMultiplexer = connectionMultiplexer;
            SubscribeKeySpace();
        }

        public IDatabase GetDatabase()
        {
            return _connectionMultiplexer.GetDatabase();
        }

        public event EventHandler<string>? KeyDeleted;

        public void SubscribeKeySpace()
        {
            ISubscriber sub = _connectionMultiplexer.GetSubscriber();

            sub.Subscribe("__keyspace@0__:*", (channel, value) =>
            {
                var channelString = (string)channel;
                if ((string)value == "del")
                {
                    int start = channelString.IndexOf(':') + 1;
                    KeyDeleted?.Invoke(this, channelString.Substring(start));
                }
            });
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

        [ThreadStatic]
        static ITransaction? _transaction;
        
        [ThreadStatic]
        static IDatabase? _database;

        public (bool, string) RunInTransaction(string failureMessage, Func<IDatabase, ITransaction, (bool,string)> toRun)
        {
            if(_transaction != null)
            {
                throw new Exception("Transaction already in progress");
            }
            _database = GetDatabase();
            _transaction = _database.CreateTransaction();
            (bool result, string message) = toRun(_database, _transaction);
            if(!result) return (result, message);
            result = _transaction.Execute();
            _transaction = null;
            _database = null;
            if(!result)
            {
                return (false, failureMessage);
            }
            return (true, "");
        }

        public ITransaction? CurrentTransaction => _transaction;
        public IDatabase? CurrentDatabase => _database;
    }
}