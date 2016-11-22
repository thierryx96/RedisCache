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

        protected IKeyExtractor<TValue> MasterKeyExtractor { get; }
        private const string CollectionMasterSuffix = "master";

        protected string CollectionRootName { get; }

        public RedisStore(
            IRedisDatabaseConnector connection,
            ISerializer serializer,
            CollectionSettings<TValue> collectionSettings)
        {
            _database = connection.GetConnectedDatabase();
            _serializer = serializer;

            CollectionRootName = (collectionSettings.Name ?? typeof(TValue).Name).ToLowerInvariant();
            CollectionMasterName = $"{CollectionRootName}:{CollectionMasterSuffix}";

            MasterKeyExtractor = collectionSettings.MasterKeyExtractor;
            Expiry = collectionSettings.Expiry;
        }

        protected string CollectionMasterName { get; }
        protected TimeSpan? Expiry { get; }

        public string ExtractMasterKey(TValue value) => MasterKeyExtractor.ExtractKey(value);

        public TValue Get(string key)
        {
            var jsonValue = _database.HashGet(CollectionMasterName, key);
            if (!jsonValue.HasValue) return default(TValue);
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return item;
        }

        public TValue[] GetAll()
        {
            var jsonValues = _database.HashValues(CollectionMasterName);
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue)).ToArray();
            return items;
        }

        public async Task<TValue> GetAsync(string key)
        {
            var jsonValue = await _database.HashGetAsync(CollectionMasterName, key);
            if (!jsonValue.HasValue) return default(TValue);
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return item;
        }

        public virtual async Task<TValue[]> GetAllAsync()
        {
            var jsonValues = await _database.HashValuesAsync(CollectionMasterName);
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue));
            return items.ToArray();
        }

        public virtual async Task ClearAsync()
        {
            await _database.KeyDeleteAsync(CollectionMasterName);
        }

        public virtual void Set(IEnumerable<TValue> items)
        {
            var entries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));
            _database.HashSet(CollectionMasterName, entries);
            if (Expiry.HasValue)
            {
                _database.KeyExpire(CollectionMasterName, Expiry);
            }
        }

        public virtual void AddOrUpdate(TValue item)
        {
            _database.HashSet(CollectionMasterName, ExtractMasterKey(item), _serializer.Serialize(item));
            if (Expiry.HasValue)
            {
                _database.KeyExpire(CollectionMasterName, Expiry);
            }
        }

        public virtual async Task SetAsync(IEnumerable<TValue> items)
        {
            var entries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));
            await _database.HashSetAsync(CollectionMasterName, entries);
            if (Expiry.HasValue)
            {
                await _database.KeyExpireAsync(CollectionMasterName, Expiry);
            }
        }

        public virtual async Task AddOrUpdateAsync(TValue item)
        {
            await _database.HashSetAsync(CollectionMasterName, ExtractMasterKey(item), _serializer.Serialize(item));
            if (Expiry.HasValue)
            {
                await _database.KeyExpireAsync(CollectionMasterName, Expiry);
            }
        }

        public virtual async Task RemoveAsync(string key)
        {
            await _database.HashDeleteAsync(CollectionMasterName, key);
        }

        public virtual async Task RemoveAsync(IEnumerable<string> keys)
        {
            await _database.HashDeleteAsync(CollectionMasterName, keys.ToHashKeys());
        }
    }
}