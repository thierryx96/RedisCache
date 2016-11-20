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
using StackExchange.Redis;

#pragma warning disable 4014 //disabled, it on purpose that awaitable must not be awaited on transactions (https://github.com/StackExchange/StackExchange.Redis/blob/master/Docs/Transactions.md)

namespace PEL.Framework.Redis.Store
{
    /// <summary>
    /// This store support basic indexes, and maintain them in a transactional fashion
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class RedisIndexedStore<TValue> : RedisStore<TValue>, IRedisReadIndexedStoreAsync<TValue>
    {
        private readonly IEnumerable<IIndex<TValue>> _indexManagers;

        public RedisIndexedStore(
            IRedisDatabaseConnector connection,
            ISerializer serializer,
            CollectionSettings<TValue> collectionDefinition,
            IEnumerable<IndexSettings<TValue>> indexDefinitions) : 
            base(
                connection, 
                serializer,
                collectionDefinition)
        {
            var indexFactory = new IndexFactory<TValue>(serializer, _masterKeyExtractor, _collectionRootName, Expiry);

            _indexManagers = indexDefinitions.Select(index => indexFactory.CreateIndex(index.Unique, index.WithPayload, index.Extractor));
        }

        public async Task<IEnumerable<string>> GetMasterKeysByIndexAsync<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>
        {
            var foundIndex = _indexManagers.FirstOrDefault(index => index.Extractor.GetType() == typeof(TValueExtractor));

            if (foundIndex == null) throw new ArgumentException($"A search by index must use a defined index. Index:'{typeof(TValueExtractor)}' is not defined on this collection.", nameof(TValueExtractor));

            return await foundIndex.GetMasterKeys(_database, value);                      
        }

        public async Task<IEnumerable<TValue>> GetItemsByIndexAsync<TValueExtractor>(string value)
           where TValueExtractor : IKeyExtractor<TValue>
        {
            var foundIndex = _indexManagers.FirstOrDefault(index => index.Extractor.GetType() == typeof(TValueExtractor));
            if (foundIndex == null) throw new ArgumentException($"A search by index must use a defined index. Index:'{typeof(TValueExtractor)}' is not defined on this collection.", nameof(TValueExtractor));

            if (foundIndex is IMasterValueResolver<TValue>)
            {
                return await (foundIndex as IMasterValueResolver<TValue>).GetMasterValues(_database, value);
            }
            else
            {
                var masterKeys =  await foundIndex.GetMasterKeys(_database, value);
                var values = new List<TValue>();
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
        }


        public new async Task Set(IEnumerable<TValue> items)
        {
            var mainEntries = items.ToHashEntries(ExtractMasterKey, item => _serializer.Serialize(item));

            var transaction = _database.CreateTransaction();

            // set main
            transaction.HashSetAsync(GenerateMasterName(), mainEntries);
            if (Expiry.HasValue)
            {
                transaction.KeyExpireAsync(GenerateMasterName(), Expiry);
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
            var oldItem = await GetAsync(ExtractMasterKey(item));

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.AddOrUpdate(transaction, item, oldItem);
            }

            // set main
            transaction.HashSetAsync(GenerateMasterName(), ExtractMasterKey(item), _serializer.Serialize(item));
            if (Expiry.HasValue)
            {
                transaction.KeyExpireAsync(GenerateMasterName(), Expiry);
            }

            await transaction.ExecuteAsync();
        }
    }
}