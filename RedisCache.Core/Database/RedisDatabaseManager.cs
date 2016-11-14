using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedisCache.Database
{
    public class RedisDatabaseManager
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly int _dbId;

        public RedisDatabaseManager(IConnectionMultiplexer connection, int dbId = 0)
        {
            _connection = connection;
            _dbId = dbId;
        }

        public async Task Flush()
        {
            foreach (var endPoint in _connection.GetEndPoints())
            {
                var server = _connection.GetServer(endPoint);
                await server.FlushAllDatabasesAsync();
            }
        }

        public IEnumerable<string> ScanKeys(string pattern)
        {
            foreach (var endPoint in _connection.GetEndPoints())
            {
                var server = _connection.GetServer(endPoint);
                foreach (var key in server.Keys(_dbId, pattern))
                {
                    yield return key;
                }
            }
        }


    }
}
