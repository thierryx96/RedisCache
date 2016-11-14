using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Indexing;
using PEL.Framework.Redis.Serialization;
using StackExchange.Redis;

#pragma warning disable 4014 //disabled, it on purpose that awaitable must not be awaited on transactions (https://github.com/StackExchange/StackExchange.Redis/blob/master/Docs/Transactions.md)

namespace PEL.Framework.Redis.Store
{
    /// <summary>
    /// This store support basic indexes, and maintain them in a transactional fashion
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class RedisIndexedStore<TValue> : RedisStore<TValue>, IRedisIndexedStoreAsync<TValue>
    {
        private readonly IEnumerable<IIndex<TValue>> _indexManagers;

        public RedisIndexedStore(
            IConnectionMultiplexer connectionMultiplexer,
            ISerializer serializer,
            Func<TValue, string> masterKeyExtractor,
            IEnumerable<IndexDefinition<TValue>> indexDefinitions,
            string collectionName = null,
            TimeSpan? expiry = null) : base(connectionMultiplexer, serializer, masterKeyExtractor, collectionName, expiry)
        {
            var indexFactory = new IndexFactory<TValue>(masterKeyExtractor, _collectionRootName, expiry);
            _indexManagers = indexDefinitions.Select(indexFactory.CreateIndex);
        }

        /// <summary>
        /// Get a master key by an indexed value
        /// </summary>
        public async Task<IEnumerable<string>> GetAllKeysByIndexAsync(string indexName, string value)
        {
            var foundIndex = _indexManagers.FirstOrDefault(index => string.Equals(indexName, index.Name, StringComparison.InvariantCultureIgnoreCase));

            if (foundIndex == null) throw new ArgumentException($"A search by index must use a defined index. Index:'{indexName}' is not defined on this collection.", nameof(indexName));

            return await foundIndex.GetMasterKeys(_database, value);
        }

        /// <summary>
        /// Get a stroed item by an indexed value
        /// </summary>
        public async Task<IEnumerable<TValue>> GetAllByIndexAsync(string indexName, string value)
        {
            var masterKeys = await GetAllKeysByIndexAsync(indexName, value);
            var values = new List<TValue>();
            foreach (var key in masterKeys)
            {
                values.Add(await GetAsync(key));
            }

            return values;
        }

        public new async Task SetAsync(IEnumerable<TValue> items)
        {
            var mainEntries = items.ToHashEntries(_masterKeyExtractor, item => _serializer.Serialize(item));

            var transaction = _database.CreateTransaction();

            // set main
            transaction.HashSetAsync(GenerateMasterName(), mainEntries);
            if (_expiry.HasValue)
            {
                transaction.KeyExpireAsync(GenerateMasterName(), _expiry);
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

            // flush main
            transaction.KeyDeleteAsync(GenerateMasterName());

            foreach (var index in _indexManagers)
            {
                index.Clear(transaction);
            }

            await transaction.ExecuteAsync();
        }

        public override async Task RemoveAsync(string key)
        {
            var item = await GetAsync(key);

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.Remove(transaction, new[] { item });
            }

            transaction.HashDeleteAsync(GenerateMasterName(), key);

            await transaction.ExecuteAsync();
        }

        public override async Task AddOrUpdateAsync(TValue item)
        {
            var oldItem = await GetAsync(_masterKeyExtractor(item));

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.AddOrUpdate(transaction, item, oldItem);
            }

            // set main
            transaction.HashSetAsync(GenerateMasterName(), _masterKeyExtractor(item), _serializer.Serialize(item));
            if (_expiry.HasValue)
            {
                transaction.KeyExpireAsync(GenerateMasterName(), _expiry);
            }

            await transaction.ExecuteAsync();
        }
    }
}