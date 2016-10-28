using RedisCache.Console.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
        /*
private readonly IDatabase _database;
private readonly ISerializer _serializer;


public RedisCacheStore(ISerializer serializer, IConnectionMultiplexer connectionMultiplexer)
{
    _database = connectionMultiplexer.GetDatabase();
    _serializer = serializer;

    // TODO(thierryr): learn this and implement if useful and applicable
    // this can be used to direct read/write to a slave or master when redis instances are configured in such a way
    //_readFlag = settings.PreferSlaveForRead ? CommandFlags.PreferSlave : CommandFlags.PreferMaster;

}

public static string GenerateRedisKey<T>(string itemKey)
{
    return $"{nameof(T).ToLowerInvariant()}:{itemKey}";
}

public bool ContainsKey<T>(string key)
{
    return _database.KeyExists(GenerateRedisKey<T>(key));
}

public void Remove<T>(string key)
{
    _database.KeyDelete(GenerateRedisKey<T>(key));
}

public void Add<T>(string key, T item, TimeSpan? slidingExpiryTimeout = null)
{
    _database.StringSet(GenerateRedisKey<T>(key), _serializer.Serialize(item), slidingExpiryTimeout);
}

public void AddRange<T>(IEnumerable<T> items, TimeSpan? slidingExpiryTimeout = null)
{
    _database.StringSet(GenerateRedisKey<T>(string.Empty), _serializer.Serialize(items), slidingExpiryTimeout);
}

public IEnumerable<T> GetRange<T>()
{
    var values = _database.StringGet(GenerateRedisKey<T>(string.Empty));
    var jsonValues = new JArray(values.ToString());
    return jsonValues.Select(item => (T)item.ToObject(typeof(T)));
}

public T Get<T>(string key)
{
    var value = _database.StringGet(GenerateRedisKey<T>(key));
    if (!value.HasValue) return default(T);

    //TODO(thierryr): HACK
    var jsonValue = JToken.Parse(value.ToString());

    return (T)jsonValue.ToObject(typeof(T));
}

*/
    }
}
