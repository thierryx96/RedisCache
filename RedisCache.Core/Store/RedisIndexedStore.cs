using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Indexing;
using PEL.Framework.Redis.Serialization;
using StackExchange.Redis;
#pragma warning disable 4014


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

        public async Task<IEnumerable<string>> GetKeysByIndex(string indexName, string value)
        {
            var foundIndex = _indexManagers.FirstOrDefault(index => string.Equals(indexName, index.Name, StringComparison.InvariantCultureIgnoreCase));

            if (foundIndex == null) throw new ArgumentException($"A search by index must use a defined index. Index:'{indexName}' is not defined on this collection.", nameof(indexName));

            return await foundIndex.GetMasterKeys(_database, value);
        }

        public async Task<IEnumerable<TValue>> GetItemsByIndex(string indexName, string value)
        {
            var masterKeys = await GetKeysByIndex(indexName, value);
            List<TValue> values = new List<TValue>();
            foreach (var key in masterKeys)
            {
                var item = await base.Get(key);
                if (!item.Equals(default(TValue)))
                {
                    values.Add(item);
                }
            }

            return values;
        }

        public new async Task Set(IEnumerable<TValue> items)
        {
            var mainEntries = items.ToHashEntries(_keyExtractor, item => _serializer.Serialize(item));

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
        

        public override async Task Clear()
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

        public override async Task Remove(params string[] keys)
        {
            IList<TValue> items = new List<TValue>();
            foreach (var key in keys)
            {
                items.Add(await base.Get(key));
            }

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.Remove(transaction, items);
            }

            await _database.HashDeleteAsync(GenerateMasterName(), keys.ToHashKeys());

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