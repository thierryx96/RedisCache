﻿using StackExchange.Redis;

namespace PEL.Framework.Redis.Database
{
    public abstract class RedisDatabaseConnector : IRedisDatabaseConnector
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly int _databaseId;

        private IDatabase _database;

        protected RedisDatabaseConnector(IConnectionMultiplexer multiplexer, int databaseId)
        {
            _multiplexer = multiplexer;
            _databaseId = databaseId;
        }

        public IDatabase GetConnectedDatabase() => _database ?? (_database = _multiplexer.GetDatabase(_databaseId));
    }
}