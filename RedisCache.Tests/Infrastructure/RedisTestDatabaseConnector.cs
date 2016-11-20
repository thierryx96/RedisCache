using PEL.Framework.Redis.Database;
using StackExchange.Redis;

namespace PEL.Framework.Redis.IntegrationTests.Infrastructure
{
    internal class RedisTestDatabaseConnector : RedisDatabaseConnector
    {
        public RedisTestDatabaseConnector(IConnectionMultiplexer multiplexer) : base(multiplexer, 2)
        {
        }
    }
}