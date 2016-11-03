using RedisCache.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisCache.Store
{



    /// <summary>
    /// This version of a Redis store any entity type as a hashset, the key being the C# entity type
    /// and the value being a dictionary of entities (of this type), with their specific domain key
    /// Car : [ key1 -> (name : Toyota, wheels: 4), key2 -> (name : Subaru, wheels: 4) ]
    /// HashSet's key : 'Car'
    /// HashSet's 1st Element Key : 'key1'
    /// HashSet's 2nd Element Key : 'key2'
    /// </summary>
    public class RedisCacheStore<TValue>
    {
        protected readonly IDatabase _database;
        private readonly ISerializer _serializer;
        protected Func<TValue, string> _keyExtractor;
        protected readonly string _collectionName;
        protected readonly TimeSpan? _expiry; 

        public RedisCacheStore(
            IConnectionMultiplexer connectionMultiplexer, 
            ISerializer serializer,
            Func<TValue, string> keyExtractor,
            TimeSpan? expiry)

          //  IDictionary<string, Func<TValue, string>> indexExtractors)
        {
            _database = connectionMultiplexer.GetDatabase();
            _serializer = serializer;
            _keyExtractor = keyExtractor;
            _expiry = expiry;
            //_indexExtractors = indexExtractors;
            _collectionName = $"{typeof(TValue).Name.ToLowerInvariant()}";

            // TODO(thierryr): learn this and implement if useful and applicable
            // this can be used to direct read/write to a slave or master when redis instances are configured in such a way
            //_readFlag = settings.PreferSlaveForRead ? CommandFlags.PreferSlave : CommandFlags.PreferMaster;
        }

        //private static string GenerateCollectionKey() => $"{typeof(TValue).Name.ToLowerInvariant()}";

        public virtual async Task Set(IEnumerable<TValue> items)
        {
            var entries = items.Select(item => new HashEntry(_keyExtractor(item), _serializer.Serialize(item))).ToArray();
            await _database.HashSetAsync(_collectionName, entries);

            if (_expiry.HasValue)
            {
                await _database.KeyExpireAsync(_collectionName, _expiry);
            }
        }

        public virtual async Task<TValue> Get(string key)
        {
            var jsonValue = await _database.HashGetAsync(_collectionName, key);
            if (!jsonValue.HasValue) return default(TValue);
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return item;
        }

        public virtual async Task<IEnumerable<TValue>> GetAll()
        {
            var jsonValues = await _database.HashValuesAsync(_collectionName);
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue));
            return items;
        }

        public virtual async Task AddOrUpdate(TValue item)
        {
            var entry = new HashEntry(_keyExtractor(item), _serializer.Serialize(item));
            await _database.HashSetAsync(_collectionName, new[] { entry });

            if (_expiry.HasValue)
            {
                await _database.KeyExpireAsync(_collectionName, _expiry);
            }
        }

        public virtual async Task Remove(string key)
        {
            await _database.HashDeleteAsync(_collectionName, key);
        }

        public virtual async Task Flush()
        {
            await _database.KeyDeleteAsync(_collectionName);
        }
    }
}