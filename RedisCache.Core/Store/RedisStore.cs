using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Database;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Serialization;
using PEL.Framework.Redis.Store.Contracts;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Store
{
    /// <summary>
    /// Simple Redis Store implementation, store and manage a collection of keys (master keys)
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class RedisStore<TValue> : IRedisExpirableStore<TValue>
    {
        protected readonly ISerializer _serializer;
        protected readonly IDatabase _database;

        protected readonly string _collectionRootName;
        protected readonly IKeyExtractor<TValue> _masterKeyExtractor; 
        private const string CollectionMasterSuffix = "master";

        public string CollectionRootName => _collectionRootName;

        public RedisStore(
            IRedisDatabaseConnector connection,
            ISerializer serializer,
            CollectionSettings<TValue> collectionDefinition)
        {
            _database = connection.GetConnectedDatabase();
            _serializer = serializer;

            _collectionRootName = (collectionDefinition.Name ?? typeof(TValue).Name).ToLowerInvariant();
            _masterKeyExtractor = collectionDefinition.MasterKeyExtractor;
            Expiry = collectionDefinition.Expiry;
        }

        protected string GenerateMasterName() => $"{_collectionRootName}:{CollectionMasterSuffix}";
        protected TimeSpan? Expiry { get; }

        public string ExtractMasterKey(TValue value) => _masterKeyExtractor.ExtractKey(value);

        public TValue Get(string key)
        {
            var jsonValue = _database.HashGet(GenerateMasterName(), key);
            if (!jsonValue.HasValue) return default(TValue);
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return item;
        }

        public IEnumerable<TValue> GetAll()
        {
            var jsonValues = _database.HashValues(GenerateMasterName());
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue));
            return items;
        }

        public async Task<TValue> GetAsync(string key)
        {
            var jsonValue = await _database.HashGetAsync(GenerateMasterName(), key);
            if (!jsonValue.HasValue) return default(TValue);
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return item;
        }

        public virtual async Task<IEnumerable<TValue>> GetAllAsync()
        {
            var jsonValues = await _database.HashValuesAsync(GenerateMasterName());
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue));
            return items;
        }

        public virtual async Task ClearAsync()
        {
            await _database.KeyDeleteAsync(GenerateMasterName());
        }

        public virtual void Set(IEnumerable<TValue> items)
        {
            var entries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));
            _database.HashSet(GenerateMasterName(), entries);
            if (Expiry.HasValue)
            {
                _database.KeyExpire(GenerateMasterName(), Expiry);
            }
        }

        public virtual async Task SetAsync(IEnumerable<TValue> items)
        {
            var entries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));
            await _database.HashSetAsync(GenerateMasterName(), entries);
            if (Expiry.HasValue)
            {
                await _database.KeyExpireAsync(GenerateMasterName(), Expiry);
            }
        }

        public virtual async Task AddOrUpdateAsync(TValue item)
        {
            await _database.HashSetAsync(GenerateMasterName(), ExtractMasterKey(item), _serializer.Serialize(item));
            if (Expiry.HasValue)
            {
                await _database.KeyExpireAsync(GenerateMasterName(), Expiry);
            }
        }

        public virtual async Task RemoveAsync(string key)
        {
            await _database.HashDeleteAsync(GenerateMasterName(), key);
        }

        public virtual async Task RemoveAsync(IEnumerable<string> keys)
        {
            await _database.HashDeleteAsync(GenerateMasterName(), keys.ToHashKeys());
        }
    }
}