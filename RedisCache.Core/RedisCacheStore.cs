using RedisCache.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

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
        protected readonly ISerializer _serializer;
        protected readonly IDatabase _database;
        protected Func<TValue, string> _keyExtractor;
        protected readonly string _collectionRootName;
        protected readonly TimeSpan? _expiry;

        private const string CollectionMasterSuffix = "master";
        private const string CollectionDefinitionSuffix = "def";


        public RedisCacheStore(
            IConnectionMultiplexer connectionMultiplexer, 
            ISerializer serializer,
            Func<TValue, string> keyExtractor,
            TimeSpan? expiry)
        {
            _database = connectionMultiplexer.GetDatabase();
            _serializer = serializer;
            _keyExtractor = keyExtractor;
            _expiry = expiry;
            _collectionRootName = $"{typeof(TValue).Name.ToLowerInvariant()}";
        }

        protected string GenerateMasterName() => $"{_collectionRootName}:{CollectionMasterSuffix}";
        protected string GenerateDefinitionName() => $"{_collectionRootName}:{CollectionDefinitionSuffix}"; //TODO: use later to store collection metadata (such as is empty, last updated etc ...)

        public virtual async Task Flush()
        {
            await _database.KeyDeleteAsync(GenerateMasterName());
        }

        public virtual async Task Set(IEnumerable<TValue> items)
        {
            var entries = items.Select(item => new HashEntry(_keyExtractor(item), _serializer.Serialize(item))).ToArray();
            await _database.HashSetAsync(GenerateMasterName(), entries);
            if (_expiry.HasValue)
            {
                await _database.KeyExpireAsync(GenerateMasterName(), _expiry);
            }
        }        

        public virtual async Task<TValue> Get(string key)
        {
            var jsonValue = await _database.HashGetAsync(GenerateMasterName(), key);
            if (!jsonValue.HasValue) return default(TValue);
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return item;
        }

        public virtual async Task<IEnumerable<TValue>> GetAll()
        {
            var jsonValues = await _database.HashValuesAsync(GenerateMasterName());
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue));
            return items;
        }

        public virtual async Task AddOrUpdate(TValue item)
        {
            await _database.HashSetAsync(GenerateMasterName(), _keyExtractor(item), _serializer.Serialize(item));
            if (_expiry.HasValue)
            {
                await _database.KeyExpireAsync(GenerateMasterName(), _expiry);
            }
        }

        public virtual async Task Remove(string key)
        {
            await _database.HashDeleteAsync(GenerateMasterName(), key);
        }
    }
}