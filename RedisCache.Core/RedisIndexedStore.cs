using RedisCache.Serialization;
using StackExchange.Redis;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisCache.Store
{
    //TODO: bundle writes into transactions
    //var tran = db.CreateTransaction();
    //tran.AddCondition(Condition.HashNotExists(custKey, "UniqueID"));
    //tran.HashSetAsync(custKey, "UniqueID", newId);
    //bool committed = tran.Execute();
    public class RedisIndexedStore<TValue> : RedisCacheStore<TValue>
    {       
        private readonly IDictionary<string, Func<TValue, string>> _indexExtractors;

        public RedisIndexedStore(
            IConnectionMultiplexer connectionMultiplexer, 
            ISerializer serializer,
            Func<TValue, string> keyExtractor,
            IDictionary<string, Func<TValue, string>> indexExtractors,
            TimeSpan? expiry) : base(connectionMultiplexer, serializer, keyExtractor, expiry)            
        {
             _indexExtractors = indexExtractors;
        }



        private string GenerateIndexName(string indexName) => $"{this._collectionRootName}:idx_{indexName.ToLowerInvariant()}";
        private string GenerateReverseIndexName(string indexName) => $"{this._collectionRootName}:rev_{indexName.ToLowerInvariant()}";


        public virtual async Task<string> GetKeyByIndex(string indexName, string value)
        {
            return await _database.HashGetAsync(GenerateIndexName(indexName), value);
        }

        //private string GetIndexByKey(string indexName, string key)
        //{
        //    return _database.HashGet(GenerateReverseIndexName(indexName), key);
        //}

        public virtual async Task<TValue> GetValueByIndex(string indexName, string value)
        {
            var key = await GetKeyByIndex(indexName, value);
            if (key == null) return default(TValue);
            return await Get(key);
        }


        //private Task SetIndex(IDatabaseAsync database, string indexName, HashEntry[] entries, Func<TValue, string> indexExtractor)
        //{
        //    return database.HashSetAsync(GenerateIndexName(indexName), entries, CommandFlags.DemandMaster).ContinueWith(
        //        (task) =>
        //        {
        //            if (_expiry.HasValue)
        //            {
        //                database.KeyExpireAsync(GenerateIndexName(indexName), _expiry);
        //            }
        //        });
        //}

        //private async Task ClearIndexesForKey(IDatabaseAsync database, string key)
        //{
        //    foreach (var indexExtractor in _indexExtractors)
        //    {
        //        var indexEntries = await _database.HashGetAllAsync(GenerateIndexName(indexExtractor.Key));
        //        var indexKey = indexEntries.FirstOrDefault(entry => entry.Value.Equals(key)).Name;
        //        if (indexKey.HasValue)
        //        {
        //            await database.HashDeleteAsync(GenerateIndexName(indexExtractor.Key), indexKey);
        //        }
        //    }
        //}

        public Task ExecuteSet(IEnumerable<TValue> items)
        {
            var mainItems = items.ToArray();
            var mainEntries = mainItems.Select(item => new HashEntry(_keyExtractor(item), _serializer.Serialize(item))).ToArray();

            var transaction = _database.CreateTransaction();

            // set main
            transaction.HashSetAsync(GenerateMasterName(), mainEntries);
            if (_expiry.HasValue)
            {
                _database.KeyExpireAsync(GenerateMasterName(), _expiry);
            }

            // set indexes
            foreach (var index in _indexExtractors)
            {
                var indexName = GenerateIndexName(index.Key);
                var reverseIndexName = GenerateReverseIndexName(index.Key);

                var indexedEntries = mainItems.Select(item => new HashEntry(index.Value(item), _keyExtractor(item))).ToArray();
                var reverseIndexesEntries = mainItems.Select(item => new HashEntry(_keyExtractor(item), index.Value(item))).ToArray();

                transaction.HashSetAsync(indexName, indexedEntries);
                transaction.HashSetAsync(indexName, reverseIndexesEntries);

                if (_expiry.HasValue)
                {
                    _database.KeyExpireAsync(indexName, _expiry);
                    _database.KeyExpireAsync(reverseIndexName, _expiry);
                }
            }

            return transaction.ExecuteAsync();
        }


        public Task<bool> ExecuteAddOrUpdate(TValue item)
        {
            var key = _keyExtractor(item);
            // fetch impacted indexes (name -> key)
            var impactedIndexEntries = _indexExtractors
                .ToDictionary(index => index.Key,
                    index => _database.HashGet(GenerateReverseIndexName(index.Key), key));

            var transaction = _database.CreateTransaction();

            // add main
            transaction.HashSetAsync(GenerateMasterName(), key, _serializer.Serialize(item));

            foreach (var indexEntry in impactedIndexEntries)
            {

                var indexName = GenerateIndexName(indexEntry.Key);
                var reverseName = GenerateReverseIndexName(indexEntry.Key);

                // remove indexes entries
                if (indexEntry.Value.HasValue)
                {
                    transaction.HashDeleteAsync(indexName, indexEntry.Value);
                    transaction.HashDeleteAsync(reverseName, key);
                }

                // add new entries
                var newIndexValue = _indexExtractors[indexEntry.Key](item);

                transaction.HashSetAsync(indexName, newIndexValue, key);
                transaction.HashSetAsync(reverseName, key, newIndexValue);

                // expiry
                if (_expiry.HasValue)
                {
                    _database.KeyExpireAsync(indexName, _expiry);
                    _database.KeyExpireAsync(reverseName, _expiry);
                }

            }

            // main expiry

            if (_expiry.HasValue)
            {
                transaction.KeyExpireAsync(GenerateMasterName(), _expiry);
            }

            return transaction.ExecuteAsync();
        }


        public Task<bool> ExecuteRemove(string key)
        {
            // fetch impacted indexes (name -> key)
            var impactedIndexEntries = _indexExtractors.ToDictionary(index => index.Key,
                index => _database.HashGet(GenerateReverseIndexName(index.Key), key));

            var transaction = _database.CreateTransaction();
             
            // remove main
            transaction.HashDeleteAsync(GenerateMasterName(), key);

            // remove indexes entries
            foreach (var indexEntries in impactedIndexEntries)
            {
                transaction.HashDeleteAsync(GenerateIndexName(indexEntries.Key), indexEntries.Value);
                transaction.HashDeleteAsync(GenerateReverseIndexName(indexEntries.Key), key);
            }

            return transaction.ExecuteAsync();
        }

        public Task<bool> ExecuteFlush()
        {
            var transaction = _database.CreateTransaction();

            // flush main
            transaction.KeyDeleteAsync(GenerateMasterName());

            foreach (var indexExtractor in _indexExtractors)
            {
                transaction.KeyDeleteAsync(GenerateIndexName(indexExtractor.Key));
            }

            return transaction.ExecuteAsync();
        }

        public new async Task Remove(string key) => await ExecuteRemove(key);
        public new async Task AddOrUpdate(TValue item) => await ExecuteAddOrUpdate(item);
        public new async Task Flush() => await ExecuteFlush();
        public new async Task Set(IEnumerable<TValue> items) => await ExecuteSet(items);


    }
}