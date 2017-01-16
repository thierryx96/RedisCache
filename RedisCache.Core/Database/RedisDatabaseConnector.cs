using StackExchange.Redis;

namespace PEL.Framework.Redis.Database
{
    public class RedisDatabaseConnector : IRedisDatabaseConnector
    {
        private readonly int _databaseId;
        private readonly IConnectionMultiplexer _multiplexer;

        private IDatabase _database;

        public RedisDatabaseConnector(IConnectionMultiplexer multiplexer, int databaseId)
        {
            _multiplexer = multiplexer;
            _databaseId = databaseId;
        }

        public IDatabase GetConnectedDatabase() => _database ?? (_database = _multiplexer.GetDatabase(_databaseId));
    }
}