using RedisCache.Console.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console
{
    public interface ICacheStore
    {
        bool Exists(string key);
        T Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan expiredIn);
        void Remove(string key);

        Task<bool> ExistsAsync(string key);
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan expiredIn);
        Task RemoveAsync(string key);
    }

    public class RedisCacheStore : ICacheStore
    {
        private readonly IDatabase _database;
        private readonly ISerializer _serializer;
        private readonly CommandFlags _readFlag;

        public RedisCacheStore(ISerializer serializer, IConnectionMultiplexer connectionMultiplexer)
        {
            _database = connectionMultiplexer.GetDatabase();

            _serializer = serializer;
            //_readFlag = settings.PreferSlaveForRead ? CommandFlags.PreferSlave : CommandFlags.PreferMaster;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var result = await _database.StringGetAsync(key, _readFlag);

            return _serializer.Deserialize<T>(result);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiredIn)
        {
            await _database.StringSetAsync(key, _serializer.Serialize(value), expiredIn);
        }

        public async Task RemoveAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }

        public bool Exists(string key)
        {
            return _database.KeyExists(key, _readFlag);
        }

        public T Get<T>(string key)
        {
            return _serializer.Deserialize<T>(_database.StringGet(key, CommandFlags.PreferSlave));
        }

        public void Set<T>(string key, T value, TimeSpan expiredIn)
        {
            _database.StringSet(key, _serializer.Serialize(value), expiredIn);
        }

        public void Remove(string key)
        {
            _database.KeyDelete(key);
        }
    }
}
