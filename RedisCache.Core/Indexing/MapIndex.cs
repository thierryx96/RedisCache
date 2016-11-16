using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedisCache.Indexing
{
    public class MapIndex<TValue> : IIndex<TValue>, IKeyResolver
    {
        private readonly TimeSpan? _expiry;
        public string Name { get; private set; }
        public Func<TValue, string> IndexKeyExtractor;

        public Func<TValue, string> KeyExtractor => IndexKeyExtractor;

        private string GenerateSetCollectionName(string key) => $"{_indexCollectionPrefix}[{key}]";
        private readonly Func<TValue, string> _masterKeyExtractor;
        private readonly string _indexCollectionPrefix;

        public MapIndex(
            string indexName,
            Func<TValue, string> indexKeyExtractor,
            Func<TValue, string> masterKeyExtractor,
            string collectionRootName,
            TimeSpan? expiry)
        {
            Name = indexName;
            IndexKeyExtractor = indexKeyExtractor;
            _masterKeyExtractor = masterKeyExtractor;
            _indexCollectionPrefix = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && IndexKeyExtractor(oldItem)!= null &&  !IndexKeyExtractor(newItem).Equals(IndexKeyExtractor(oldItem)))
            {
                Remove(context, new[] { oldItem });
            };

            if(IndexKeyExtractor(newItem) != null)
            {
                Set(context, new[] { newItem });
            }
        }

        public async Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string indexKey)
        {
            var masterKeys = await context.SetMembersAsync(GenerateSetCollectionName(indexKey));
            return masterKeys.Select(masterKey => masterKey.ToString()).ToArray();
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            foreach (var item in items)
            {
                context.SetRemoveAsync(GenerateSetCollectionName(IndexKeyExtractor(item)), _masterKeyExtractor(item));
            }
        }

        public void Clear(
            IDatabaseAsync context
            )
        {
            // Not at problem if the index is stale as long as the master key is removed.
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
            )
        {
            var indexEntries = items.ToMappings(IndexKeyExtractor, _masterKeyExtractor).ToArray();

            foreach (var entry in indexEntries)
            {
                context.SetAddAsync(GenerateSetCollectionName(entry.Key), entry.ToArray());
                context.KeyExpireAsync(entry.Key, _expiry);
            }        
        }


    }

}
