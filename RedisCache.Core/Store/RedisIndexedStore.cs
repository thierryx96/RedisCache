using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Database;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing;
using PEL.Framework.Redis.Serialization;
using PEL.Framework.Redis.Store.Contracts;
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
            IRedisDatabaseConnector connector,
            ISerializer serializer,
            Func<TValue, string> masterKeyExtractor,
            IEnumerable<IndexSettings<TValue>> indexDefinitions,
            string collectionName = null,
            TimeSpan? expiry = null) : base(connector, serializer, masterKeyExtractor, collectionName, expiry)
        {
            _masterKeyExtractor = masterKeyExtractor;
            var indexFactory = new IndexFactory<TValue>(masterKeyExtractor, _collectionRootName, expiry);
            _indexManagers = indexDefinitions.Select(index => indexFactory.CreateIndex(index.Unique, index.Extractor));
        }

        public async Task<IEnumerable<string>> GetMasterKeysByIndexAsync<TValueExtractor>(string value)
                       where TValueExtractor : IKeyExtractor<TValue>
        {
            // get by type of extractor, 

            var foundIndex = _indexManagers.FirstOrDefault(index => index.Extractor.GetType() == typeof(TValueExtractor));

            if (foundIndex == null) throw new ArgumentException($"A search by index must use a defined index. Index:'{typeof(TValueExtractor)}' is not defined on this collection.", nameof(TValueExtractor));

            return await foundIndex.GetMasterKeys(_database, value);
        }

        public async Task<IEnumerable<TValue>> GetItemsByIndexAsync<TValueExtractor>(string value)
           where TValueExtractor : IKeyExtractor<TValue>
        {

            var masterKeys = await GetMasterKeysByIndexAsync<TValueExtractor>(value);
            List<TValue> values = new List<TValue>();
            foreach (var key in masterKeys)
            {
                var item = await base.GetAsync(key);
                if (!item.Equals(default(TValue)))
                {
                    values.Add(item);
                }
            }

            return values;
        }

        //public async Task<IEnumerable<TValue>> GetValuesByIndexAsync(string indexName, string value)
        //{
        //    var masterKeys = await GetKeysByIndexAsync(indexName, value);
        //    List<TValue> values = new List<TValue>();
        //    foreach (var key in masterKeys)
        //    {
        //        var item = await base.GetAsync(key);
        //        if (!item.Equals(default(TValue)))
        //        {
        //            values.Add(item);
        //        }
        //    }

        //    return values;
        //}

        public new async Task Set(IEnumerable<TValue> items)
        {
            var mainEntries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));

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


        //public override string ExtractMasterKey(TValue value) => _masterKeyExtractor(value);

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

        public override async Task RemoveAsync(IEnumerable<string> keys)
        {
            IList<TValue> items = new List<TValue>();
            foreach (var key in keys)
            {
                var indexedKey = await base.GetAsync(key);
                if (!indexedKey.Equals(default(TValue)))
                {
                    items.Add(indexedKey);

                }
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