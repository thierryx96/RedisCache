using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Database;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing;
using PEL.Framework.Redis.Indexing.Writers;
using PEL.Framework.Redis.Serialization;
using PEL.Framework.Redis.Store.Contracts;

#pragma warning disable 4014 //disabled, it on purpose that awaitable must not be awaited on transactions (https://github.com/StackExchange/StackExchange.Redis/blob/master/Docs/Transactions.md)

namespace PEL.Framework.Redis.Store
{
    /// <summary>
    /// This store support basic indexes, and maintain them in a transactional fashion
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class RedisIndexedStore<TValue> : RedisStore<TValue>, IRedisExpirableIndexedStore<TValue>
    {
        private readonly IEnumerable<IIndexWriter<TValue>> _indexManagers;

        public RedisIndexedStore(
            IRedisDatabaseConnector connection,
            ISerializer serializer,
            CollectionWithIndexesSettings<TValue> settings) :
            base(
                connection,
                serializer,
                settings)
        {
            var indexFactory = new IndexFactory<TValue>(CollectionRootName, Expiry, serializer);
            _indexManagers = settings.Indexes.Select(indexDefinition => indexFactory.CreateIndex(indexDefinition.Unique, indexDefinition.WithPayload, indexDefinition.Extractor, indexDefinition.Name));
        }

        #region "Async Read API"

        public async Task<string[]> GetMasterKeysByIndexAsync<TValueExtractor>(string indexedKey)
            where TValueExtractor : IKeyExtractor<TValue>
        => (await GetItemsByIndexAsync<TValueExtractor>(indexedKey)).Select(ExtractMasterKey).ToArray();

        public async Task<TValue[]> GetItemsByIndexAsync<TValueExtractor>(string indexedKey)
            where TValueExtractor : IKeyExtractor<TValue>
        {
            var foundIndex = GetIndexForExtractor<TValueExtractor>();
            return await foundIndex.GetMasterValuesAsync(_database, indexedKey);
        }

        public async Task<IDictionary<string, string[]>> GetMasterKeysByIndexAsync<TValueExtractor>(IEnumerable<string> indexedKeys) where TValueExtractor : IKeyExtractor<TValue>
        => (await GetItemsByIndexAsync<TValueExtractor>(indexedKeys)).ToDictionary(item => item.Key, item => item.Value.Select(ExtractMasterKey).ToArray());

        public async Task<IDictionary<string, TValue[]>> GetItemsByIndexAsync<TValueExtractor>(IEnumerable<string> indexedKeys) where TValueExtractor : IKeyExtractor<TValue>
        {
            var foundIndex = GetIndexForExtractor<TValueExtractor>();
            return await foundIndex.GetMasterValuesAsync(_database, indexedKeys);
        }

        #endregion

        #region "Sync Read API"

        public string[] GetMasterKeysByIndex<TValueExtractor>(string indexedKey) where TValueExtractor : IKeyExtractor<TValue>
        => GetItemsByIndex<TValueExtractor>(indexedKey).Select(ExtractMasterKey).ToArray();

        public TValue[] GetItemsByIndex<TValueExtractor>(string indexedKey)
            where TValueExtractor : IKeyExtractor<TValue>
        {
            var foundIndex = GetIndexForExtractor<TValueExtractor>();
            return foundIndex.GetMasterValues(_database, indexedKey);
        }

        public IDictionary<string, string[]> GetMasterKeysByIndex<TValueExtractor>(IEnumerable<string> indexedKeys) where TValueExtractor : IKeyExtractor<TValue>
        => (GetItemsByIndex<TValueExtractor>(indexedKeys)).ToDictionary(item => item.Key, item => item.Value.Select(ExtractMasterKey).ToArray());

        public IDictionary<string, TValue[]> GetItemsByIndex<TValueExtractor>(IEnumerable<string> indexedKeys) where TValueExtractor : IKeyExtractor<TValue>
        {
            var foundIndex = GetIndexForExtractor<TValueExtractor>();
            return foundIndex.GetMasterValues(_database, indexedKeys);
        }

        #endregion

        #region "Async Write API"
            

        public override async Task SetAsync(IEnumerable<TValue> items)
        {
            var mainEntries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));

            var transaction = _database.CreateTransaction();

            // set main
            transaction.HashSetAsync(CollectionMasterName, mainEntries);
            if (Expiry.HasValue)
            {
                transaction.KeyExpireAsync(CollectionMasterName, Expiry);
            }

            // set indexes
            foreach (var index in _indexManagers)
            {
                index.Set(transaction, items);
            }

            await transaction.ExecuteAsync();
        }

        public override async Task ClearAsync()
        {
            var transaction = _database.CreateTransaction();

            if (_indexManagers.Any())
            {
                // all main items to be cleared
                var items = await GetAllAsync();
                foreach (var index in _indexManagers)
                {
                    index.Remove(transaction, items);
                }
            }

            // flush main items
            transaction.KeyDeleteAsync(CollectionMasterName);

            await transaction.ExecuteAsync();
        }

        public override async Task RemoveAsync(IEnumerable<string> keys)
        {
            IList<TValue> oldItems = new List<TValue>();
            foreach (var key in keys)
            {
                var oldItem = await GetAsync(key);
                if (!oldItem.Equals(default(TValue)))
                {
                    oldItems.Add(oldItem);
                }
            }

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.Remove(transaction, oldItems);
            }

            await _database.HashDeleteAsync(CollectionMasterName, keys.ToHashKeys());

            await transaction.ExecuteAsync();
        }

        public override async Task AddOrUpdateAsync(TValue item)
        {
            var oldItem = await GetAsync(ExtractMasterKey(item));

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.AddOrUpdate(transaction, item, oldItem);
            }

            // set main
            transaction.HashSetAsync(CollectionMasterName, ExtractMasterKey(item), _serializer.Serialize(item));
            if (Expiry.HasValue)
            {
                transaction.KeyExpireAsync(CollectionMasterName, Expiry);
            }

            await transaction.ExecuteAsync();
        }

        #endregion

        #region "Sync API"
        public override void Set(IEnumerable<TValue> items)
        {
            var mainEntries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));
            var transaction = _database.CreateTransaction();

            // set main
            transaction.HashSetAsync(CollectionMasterName, mainEntries);
            if (Expiry.HasValue)
            {
                transaction.KeyExpireAsync(CollectionMasterName, Expiry);
            }

            // set indexes
            foreach (var index in _indexManagers)
            {
                index.Set(transaction, items);
            }

            transaction.Execute();
        }

        public override void AddOrUpdate(TValue item)
        {
            var oldItem = Get(ExtractMasterKey(item));

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.AddOrUpdate(transaction, item, oldItem);
            }

            // set main
            transaction.HashSetAsync(CollectionMasterName, ExtractMasterKey(item), _serializer.Serialize(item));
            if (Expiry.HasValue)
            {
                transaction.KeyExpireAsync(CollectionMasterName, Expiry);
            }

            transaction.Execute();
        }

        #endregion

        private IIndexWriter<TValue> GetIndexForExtractor<TValueExtractor>()
        {
            var foundIndex = _indexManagers.FirstOrDefault(index => index.IndexedKeyExtractor.GetType() == typeof(TValueExtractor));
            if (foundIndex == null) throw new ArgumentException($"A search by index must use a defined index. Index:'{typeof(TValueExtractor)}' is not defined on this collection.", nameof(TValueExtractor));
            return foundIndex;
        }
    }
}