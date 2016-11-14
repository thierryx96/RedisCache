using RedisCache.Indexing;
using RedisCache.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable 4014

namespace RedisCache.Store
{
    /// <summary>
    /// This store support basic indexes, and maintain them in a transactional fashion
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class RedisIndexedStore<TValue> : RedisCacheStore<TValue>, IRedisIndexedStore<TValue>
    {       
        private readonly IEnumerable<IIndex<TValue>>  _indexManagers;

        public RedisIndexedStore(
            IConnectionMultiplexer connectionMultiplexer, 
            ISerializer serializer,
            Func<TValue, string> keyExtractor,
            IEnumerable<IndexDefinition<TValue>> indexDefinitions,
            string masterCollectionName = null,
            TimeSpan? expiry = null,
            Func<TValue, string> keyExpiryExtractor = null,
            int dbId = 0
            ) : base(connectionMultiplexer, serializer, keyExtractor, masterCollectionName, expiry, dbId)            
        {
            var indexFactory = new IndexFactory<TValue>(keyExtractor, _collectionRootName, expiry);

            //if (keyExpiryExtractor != null)
            //{
            //    var keyExpiry = MasterKeyExtractor
            //}

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

        public override async Task AddOrUpdate(TValue item){
            var oldItem = await base.Get(_keyExtractor(item));

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.AddOrUpdate(transaction, item, oldItem);
            }

            // set main
            transaction.HashSetAsync(GenerateMasterName(), _keyExtractor(item), _serializer.Serialize(item));
            if (_expiry.HasValue)
            {
                transaction.KeyExpireAsync(GenerateMasterName(), _expiry);
            }

            await transaction.ExecuteAsync();
        }



    }
}
 