using RedisCache.Indexing;
using RedisCache.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisCache.Store
{
    public class RedisIndexedStore<TValue> : RedisCacheStore<TValue>
    {       
        private readonly IEnumerable<IIndex<TValue>>  _indexManagers;
        //private readonly IRedisReadStore<TValue> _masterReadStore;

        public RedisIndexedStore(
            IConnectionMultiplexer connectionMultiplexer, 
            ISerializer serializer,
            Func<TValue, string> keyExtractor,
            IEnumerable<IndexDefinition<TValue>> indexDefinitions,
            TimeSpan? expiry) : base(connectionMultiplexer, serializer, keyExtractor, expiry)            
        {
           // _masterReadStore = new RedisCacheStore<TValue>(connectionMultiplexer, serializer, keyExtractor, expiry);

            var indexFactory = new IndexFactory<TValue>(keyExtractor, _collectionRootName, expiry);
            _indexManagers = indexDefinitions.Select(indexFactory.CreateIndex);
        }

        public async Task<IEnumerable<string>> GetKeysByIndex(string indexName, string value)
        {
            var foundIndex = _indexManagers.FirstOrDefault(index => string.Equals(indexName, index.Name, StringComparison.InvariantCultureIgnoreCase));

            if (foundIndex == ) throw new ArgumentException($"A search by index must use a defined index. Index:'{indexName}' is not defined on this collection.", nameof(indexName));

            return await foundIndex.GetMasterKeys(_database, value);
        }

        public async Task<IEnumerable<TValue>> GetItemsByIndex(string indexName, string value)
        {
            var masterKeys = await GetKeysByIndex(indexName, value);
            List<TValue> values = new List<TValue>();
            foreach (var key in masterKeys)
            {
                values.Add(await base.Get(key));
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
        

        public override async Task Flush()
        {
            var transaction = _database.CreateTransaction();

            // flush main
            transaction.KeyDeleteAsync(GenerateMasterName());

            foreach (var index in _indexManagers)
            {
                index.Flush(transaction);
            }

            await transaction.ExecuteAsync();
        }

        public override async Task Remove(string key){ 
            var item = await base.Get(key);

            var transaction = _database.CreateTransaction();

            foreach (var index in _indexManagers)
            {
                index.Remove(transaction, new TValue[] { item });
            }

            transaction.HashDeleteAsync(GenerateMasterName(), key);

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
 