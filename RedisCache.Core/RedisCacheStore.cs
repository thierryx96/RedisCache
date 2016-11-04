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


        protected virtual async Task ApplySet(IDatabaseAsync database, IEnumerable<TValue> items)
        {
            var entries = items.Select(item => new HashEntry(_keyExtractor(item), _serializer.Serialize(item))).ToArray();
            await database.HashSetAsync(GenerateMasterName(), entries);
            if (_expiry.HasValue)
            {
                await database.KeyExpireAsync(GenerateMasterName(), _expiry);
            }
        }

        protected virtual async Task ApplyAddOrUpdate(IDatabaseAsync database, TValue item)
        {
            await database.HashSetAsync(GenerateMasterName(), _keyExtractor(item), _serializer.Serialize(item));
            if (_expiry.HasValue)
            {
                await database.KeyExpireAsync(GenerateMasterName(), _expiry);
            }
        }

        protected virtual async Task ApplyRemove(IDatabaseAsync database, string key)
        {
            await database.HashDeleteAsync(GenerateMasterName(), key);
        }

        protected virtual async Task ApplyFlush(IDatabaseAsync database)
        {
            await database.KeyDeleteAsync(GenerateMasterName());
        }


        public virtual async Task Set(IEnumerable<TValue> items)
        {
            await ApplySet(_database, items);
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
            await ApplyAddOrUpdate(_database, item);
        }

        public virtual async Task Remove(string key)
        {
            await ApplyRemove(_database, key);
        }

        public virtual async Task Flush()
        {
            await ApplyFlush(_database);
        }
    }
}