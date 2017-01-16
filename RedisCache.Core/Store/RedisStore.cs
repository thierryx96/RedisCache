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
    ///     Simple Redis Store implementation, store and manage a collection of keys (master keys)
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class RedisStore<TValue> : IRedisExpirableStore<TValue>
    {
        private const string CollectionMasterSuffix = "master";
        protected readonly IDatabase _database;
        protected readonly ISerializer _serializer;

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

        protected IKeyExtractor<TValue> MasterKeyExtractor { get; }

        protected string CollectionRootName { get; }

        protected string CollectionMasterName { get; }
        protected TimeSpan? Expiry { get; }

        public string ExtractMasterKey(TValue value) => MasterKeyExtractor.ExtractKey(value);

        private TValue[] GetValuesFromEntries(IEnumerable<RedisValue> entries) =>
            entries
                .Where(entry => entry.HasValue) // not found hashes are return null from redis
                .Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue))
                .ToArray();

        private TValue GetValueFromEntry(RedisValue entry) =>
            entry.HasValue ? _serializer.Deserialize<TValue>(entry) : default(TValue);

        #region "Sync Read API"

        public TValue Get(string masterKey) => GetValueFromEntry(_database.HashGet(CollectionMasterName, masterKey));

        public TValue[] Get(IEnumerable<string> masterKeys)
            => GetValuesFromEntries(_database.HashGet(CollectionMasterName, masterKeys.ToHashKeys()));

        public TValue[] GetAll() => GetValuesFromEntries(_database.HashValues(CollectionMasterName));

        #endregion

        #region "Async Read API"

        public async Task<TValue> GetAsync(string masterKey)
            => GetValueFromEntry(await _database.HashGetAsync(CollectionMasterName, masterKey));

        public async Task<TValue[]> GetAsync(IEnumerable<string> masterKeys)
            => GetValuesFromEntries(await _database.HashGetAsync(CollectionMasterName, masterKeys.ToHashKeys()));

        public async Task<TValue[]> GetAllAsync()
            => GetValuesFromEntries(await _database.HashValuesAsync(CollectionMasterName));

        #endregion

        #region "Async Write API"

        public virtual async Task ClearAsync()
        {
            await _database.KeyDeleteAsync(CollectionMasterName);
        }


        public virtual async Task SetAsync(IEnumerable<TValue> items)
        {
            var entries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));
            await _database.HashSetAsync(CollectionMasterName, entries);
            if (Expiry.HasValue)
                await _database.KeyExpireAsync(CollectionMasterName, Expiry);
        }

        public virtual async Task AddOrUpdateAsync(TValue item)
        {
            await _database.HashSetAsync(CollectionMasterName, ExtractMasterKey(item), _serializer.Serialize(item));
            if (Expiry.HasValue)
                await _database.KeyExpireAsync(CollectionMasterName, Expiry);
        }

        public virtual async Task RemoveAsync(string masterKey)
        {
            await _database.HashDeleteAsync(CollectionMasterName, masterKey);
        }

        public virtual async Task RemoveAsync(IEnumerable<string> masterKeys)
        {
            await _database.HashDeleteAsync(CollectionMasterName, masterKeys.ToHashKeys());
        }

        #endregion

        // // TODO: (trais, 28 Nov 2016) - get rid of the Sync Write API when/if no longer in use

        #region "Sync Write API"

        public virtual void Set(IEnumerable<TValue> items)
        {
            var entries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));
            _database.HashSet(CollectionMasterName, entries);
            if (Expiry.HasValue)
                _database.KeyExpire(CollectionMasterName, Expiry);
        }

        public virtual void AddOrUpdate(TValue item)
        {
            _database.HashSet(CollectionMasterName, ExtractMasterKey(item), _serializer.Serialize(item));
            if (Expiry.HasValue)
                _database.KeyExpire(CollectionMasterName, Expiry);
        }

        #endregion
    }
}