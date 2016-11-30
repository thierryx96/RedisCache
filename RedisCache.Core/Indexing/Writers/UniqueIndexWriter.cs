using System;
using System.Collections.Generic;
using System.Linq;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Writers
{
    internal class UniqueIndexWriter<TValue> : IndexWriter<TValue>
    {
        private readonly string _indexCollectionName;

        public UniqueIndexWriter(
            IKeyExtractor<TValue> indexedKeyExtractor,
            Func<TValue, string> indexedValueExtractor,
            TimeSpan? expiry,
            string indexCollectionName) : base(indexedKeyExtractor, indexedValueExtractor, expiry)
        {
            _indexCollectionName = indexCollectionName;
        }


        public override void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
        )
        {
            var indexName = _indexCollectionName;
            context.HashSetAsync(indexName, items.ToHashEntries(IndexedKeyExtractor.ExtractKey, IndexedValueExtractor));

            if (Expiry.HasValue)
                context.KeyExpireAsync(indexName, Expiry);
        }

        public override void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if ((oldItem != null) &&
                !IndexedKeyExtractor.ExtractKey(newItem)
                    .Equals(IndexedKeyExtractor.ExtractKey(oldItem), StringComparison.OrdinalIgnoreCase))
                Remove(context, new[] {oldItem});
            Set(context, new[] {newItem});
        }

        public override void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            context.HashDeleteAsync(_indexCollectionName, items.Select(IndexedKeyExtractor.ExtractKey).ToHashKeys());
        }
    }
}