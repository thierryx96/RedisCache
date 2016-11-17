using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Database;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Serialization;
using PEL.Framework.Redis.Store.Contracts;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Store
{
    /// <summary>
    /// Simple Redis Store implementation, store and manage a collection of keys (master keys)
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public abstract class RedisStore<TValue> : IRedisExpirableStore<TValue>
    {
        protected readonly ISerializer _serializer;
        protected readonly IDatabase _database;
        protected Func<TValue, string> _masterKeyExtractor;
        protected readonly string _collectionRootName;
        protected readonly TimeSpan? _expiry;
        private const string CollectionMasterSuffix = "master";

        public string CollectionRootName => _collectionRootName;
        public TimeSpan? Expiry => _expiry;

        protected RedisStore(
            IRedisDatabaseConnector connection,
            ISerializer serializer,

            // config
            Func<TValue, string> masterKeyExtractor,
            string collectionName = null,
            TimeSpan? expiry = null)
        {
            _database = connection.GetConnectedDatabase();
            _serializer = serializer;
            _masterKeyExtractor = masterKeyExtractor;
            _expiry = expiry;
            _collectionRootName = (collectionName ?? typeof(TValue).Name).ToLowerInvariant();
        }

        protected string GenerateMasterName() => $"{_collectionRootName}:{CollectionMasterSuffix}";

        public string ExtractMasterKey(TValue value) => _masterKeyExtractor(value);

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

        public async Task<IEnumerable<TValue>> GetAllAsync()
        {
            var jsonValues = await _database.HashValuesAsync(GenerateMasterName());
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue));
            return items;
        }

        public virtual async Task ClearAsync()
        {
            await _database.KeyDeleteAsync(GenerateMasterName());
        }

        public virtual async Task SetAsync(IEnumerable<TValue> items)
        {
            var entries = items.Select(item => new HashEntry(ExtractMasterKey(item), _serializer.Serialize(item))).ToArray();
            await _database.HashSetAsync(GenerateMasterName(), entries);
            if (_expiry.HasValue)
            {
                await _database.KeyExpireAsync(GenerateMasterName(), _expiry);
            }
        }

        public virtual async Task AddOrUpdateAsync(TValue item)
        {
            await _database.HashSetAsync(GenerateMasterName(), ExtractMasterKey(item), _serializer.Serialize(item));
            if (_expiry.HasValue)
            {
                await _database.KeyExpireAsync(GenerateMasterName(), _expiry);
            }
        }

        public virtual async Task RemoveAsync(IEnumerable<string> keys)
        {
            await _database.HashDeleteAsync(GenerateMasterName(), keys.ToHashKeys());
        }
    }
}