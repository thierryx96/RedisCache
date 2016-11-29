using System;
using System.Collections.Generic;
using System.Linq;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Writers
{
    internal class UniqueIndexWriter<TValue> : IIndexWriter<TValue>
    {
        private readonly Func<TValue, string> _indexedValueExtractor;
        public IKeyExtractor<TValue> IndexedKeyExtractor { get; set; }
        TimeSpan? Expiry { get; }

        private readonly string _indexCollectionName;

        public UniqueIndexWriter(
            IKeyExtractor<TValue> indexedKeyExtractor,
            Func<TValue, string> indexedValueExtractor,
            string indexCollectionName,
            TimeSpan? expiry)
        {
            _indexedValueExtractor = indexedValueExtractor;
            IndexedKeyExtractor = indexedKeyExtractor;
            _indexCollectionName = indexCollectionName;
            Expiry = expiry;
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
        )
        {
            var indexName = _indexCollectionName;
            context.HashSetAsync(indexName, items.ToHashEntries(IndexedKeyExtractor.ExtractKey, _indexedValueExtractor));

            if (Expiry.HasValue)
            {
                context.KeyExpireAsync(indexName, Expiry);
            }
        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && !IndexedKeyExtractor.ExtractKey(newItem).Equals(IndexedKeyExtractor.ExtractKey(oldItem), StringComparison.OrdinalIgnoreCase))
            {
                Remove(context, new[] { oldItem });
            }
            Set(context, new[] { newItem });
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            context.HashDeleteAsync(_indexCollectionName, items.Select(IndexedKeyExtractor.ExtractKey).ToHashKeys());
        }
    }
}