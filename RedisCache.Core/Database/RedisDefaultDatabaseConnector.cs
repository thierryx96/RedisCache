using StackExchange.Redis;

namespace PEL.Framework.Redis.Database
{
    public class RedisDefaultDatabaseConnector : RedisDatabaseConnector
    {
        public RedisDefaultDatabaseConnector(IConnectionMultiplexer multiplexer) : base(multiplexer, -1)
        {
        }
    }
}