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
        public Func<TValue, string> IndexKeyExtractor;

        public Func<TValue, string> KeyExtractor => IndexKeyExtractor;
        public string HashIndexCollectionName;

        private readonly Func<TValue, string> _masterKeyExtractor;

        public UniqueIndex(
            string indexName,
            Func<TValue, string> indexKeyExtractor,
            Func<TValue, string> masterKeyExtractor,
            string collectionRootName,
            TimeSpan? expiry)
        {
            Name = indexName;
            IndexKeyExtractor = indexKeyExtractor;
            _masterKeyExtractor = masterKeyExtractor;
            HashIndexCollectionName = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && !IndexKeyExtractor(newItem).Equals(IndexKeyExtractor(oldItem)))
            {
                Remove(context, new[] { oldItem });
            }
            Set(context, new[] { newItem });
        }


        public async Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string value)
        {
            var keyFound = await context.HashGetAsync(HashIndexCollectionName, value);
            return keyFound.HasValue ? new string[] { keyFound } : new string[0];
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            context.HashDeleteAsync(HashIndexCollectionName, items.Select(IndexKeyExtractor).ToHashKeys());
        }

        public void Clear(
            IDatabaseAsync context
            )
        {
            context.KeyDeleteAsync(HashIndexCollectionName);
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
            )
        {
            var indexName = HashIndexCollectionName;

            context.HashSetAsync(indexName, items.ToHashEntries(IndexKeyExtractor, _masterKeyExtractor));

            if (_expiry.HasValue)
            {
                context.KeyExpireAsync(indexName, _expiry);
            }
        }


    }
}
