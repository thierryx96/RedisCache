using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Indexing
{
    public class UniqueIndex<TValue> : IIndex<TValue>, IKeyResolver
    {
        private TimeSpan? _expiry;
        public string Name { get; private set; }
        public Func<TValue, string> _indexKeyExtractor;

        public Func<TValue, string> KeyExtractor => _indexKeyExtractor;
        public string _hashIndexCollectionName;

        private Func<TValue, string> _masterKeyExtractor;

        public UniqueIndex(
            string indexName,
            Func<TValue, string> indexKeyExtractor,
            Func<TValue, string> masterKeyExtractor,
            string collectionRootName,
            TimeSpan? expiry)
        {
            Name = indexName;
            _indexKeyExtractor = indexKeyExtractor;
            _masterKeyExtractor = masterKeyExtractor;
            _hashIndexCollectionName = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && !_indexKeyExtractor(newItem).Equals(_indexKeyExtractor(oldItem)))
            {
                Remove(context, new[] { oldItem });
            };
            Set(context, new[] { newItem });
        }

        // cleanup existing


        // add new indexed values

        public async Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string value)
        {
            var keyFound = await context.HashGetAsync(_hashIndexCollectionName, value);
            return keyFound.HasValue ? new string[] { keyFound } : new string[0];
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            // remove indexes entries
            context.HashDeleteAsync(_hashIndexCollectionName, items.Select(item => (RedisValue)_indexKeyExtractor(item)).ToArray());
            //return context;
        }

        public void Flush(
            IDatabaseAsync context
            )
        {
            context.KeyDeleteAsync(_hashIndexCollectionName);
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
            )
        {
            var indexName = _hashIndexCollectionName;

            context.HashSetAsync(indexName, items.ToHashEntries(_indexKeyExtractor, _masterKeyExtractor));

            if (_expiry.HasValue)
            {
                context.KeyExpireAsync(indexName, _expiry);
            }
        }


    }
}
