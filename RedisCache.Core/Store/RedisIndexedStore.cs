using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Database;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing;
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
        private readonly IEnumerable<IIndex<TValue>> _indexManagers;

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
            _indexManagers = settings.Indexes.Select(indexDefinition => indexFactory.CreateIndex(indexDefinition.Unique, indexDefinition.WithPayload, indexDefinition.Extractor));
        }

        public async Task<string[]> GetMasterKeysByIndexAsync<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>
        {
            return (await GetItemsByIndexAsync<TValueExtractor>(value)).Select(ExtractMasterKey).ToArray();
        }

        public async Task<TValue[]> GetItemsByIndexAsync<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>
        {
            var foundIndex = GetIndexForExtractor<TValueExtractor>();
            return await foundIndex.GetMasterValuesAsync(_database, value);
        }

        public string[] GetMasterKeysByIndex<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>
        {
            return GetItemsByIndex<TValueExtractor>(value).Select(ExtractMasterKey).ToArray();
        }

        public TValue[] GetItemsByIndex<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>
        {
            var foundIndex = GetIndexForExtractor<TValueExtractor>();
            return foundIndex.GetMasterValues(_database, value);
        }

        private IIndex<TValue> GetIndexForExtractor<TValueExtractor>()
        {
            var foundIndex = _indexManagers.FirstOrDefault(index => index.Extractor.GetType() == typeof(TValueExtractor));
            if (foundIndex == null) throw new ArgumentException($"A search by index must use a defined index. Index:'{typeof(TValueExtractor)}' is not defined on this collection.", nameof(TValueExtractor));
            return foundIndex;
        }

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

            foreach (var index in _indexManagers)
            {
                index.Clear(transaction);
            }

            // flush main
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
    }
}