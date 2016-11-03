using RedisCache.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisCache.Store
{
    /// <summary>
    /// This version of a Redis store any entity type as a hashset, the key being the C# entity type
    /// and the value being a dictionary of entities (of this type), with their specific domain key
    /// Car : [ key1 -> (name : Toyota, wheels: 4), key2 -> (name : Subaru, wheels: 4) ]
    /// HashSet's key : 'Car'
    /// HashSet's 1st Element Key : 'key1'
    /// HashSet's 2nd Element Key : 'key2'
    /// </summary>
    public class RedisIndexedStore<TValue> : RedisCacheStore<TValue>
    {
       
        private IDictionary<string, Func<TValue, string>> _indexExtractors;

        public RedisIndexedStore(
            IConnectionMultiplexer connectionMultiplexer, 
            ISerializer serializer,
            Func<TValue, string> keyExtractor,
            IDictionary<string, Func<TValue, string>> indexExtractors,
            TimeSpan? expiry) : base(connectionMultiplexer, serializer, keyExtractor, expiry)            
        {
             _indexExtractors = indexExtractors;

            // TODO(thierryr): learn this and implement if useful and applicable
            // this can be used to direct read/write to a slave or master when redis instances are configured in such a way
            //_readFlag = settings.PreferSlaveForRead ? CommandFlags.PreferSlave : CommandFlags.PreferMaster;
        }

        private string GenerateIndexName(string indexName) => $"{this._collectionName}:{indexName.ToLowerInvariant()}";


        private async Task SetIndex(string indexName, IEnumerable<TValue> items, Func<TValue, string> indexExtractor)
        {
            var indexedValues = items.Select(item => new HashEntry(indexExtractor(item), _keyExtractor(item))).ToArray();

            await _database.HashSetAsync(GenerateIndexName(indexName), indexedValues);

            if (_expiry.HasValue)
            {
                await _database.KeyExpireAsync(GenerateIndexName(indexName), _expiry);
            }
        }

        public override async Task Set(IEnumerable<TValue> items)
        {
            await base.Set(items);
            foreach (var indexExtractor in _indexExtractors)
            {
                await SetIndex(indexExtractor.Key, items, indexExtractor.Value);
            }            
        }

        public override async Task AddOrUpdate(TValue item)
        {
            await base.AddOrUpdate(item);
            foreach (var indexExtractor in _indexExtractors)
            {
                await SetIndex(indexExtractor.Key, new[] { item }, indexExtractor.Value);
            }
        }


        

        public virtual async Task<string> GetKeyByIndex(string indexName, string value)
        {
            return await _database.HashGetAsync(GenerateIndexName(indexName), value);
        }

        public virtual async Task<TValue> GetValueByIndex(string indexName, string value)
        {
            var key = await GetKeyByIndex(indexName, value);
            if (key == null) return default(TValue);
            return await Get(key);
        }

        public override async Task Remove(string key)
        {
            await base.Remove(key);
            foreach (var indexExtractor in _indexExtractors)
            {
                var indexEntries = await _database.HashGetAllAsync(GenerateIndexName(indexExtractor.Key));
                var indexKey = indexEntries.FirstOrDefault(entry => entry.Value.Equals(key)).Name;
                if (indexKey.HasValue)
                {
                    await _database.HashDeleteAsync(GenerateIndexName(indexExtractor.Key), indexKey);

                }
            }
        }

        public override async Task Flush()
        {
            await base.Flush();
            foreach (var indexExtractor in _indexExtractors)
            {
                await _database.KeyDeleteAsync(GenerateIndexName(indexExtractor.Key));
            }

        }
    }
}