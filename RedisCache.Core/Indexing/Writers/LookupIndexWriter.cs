using System;
using System.Collections.Generic;
using System.Linq;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Writers
{
    internal class LookupIndexWriter<TValue> : IndexWriter<TValue>
    {
        private readonly Func<string, string> _collectionNameGenerator;

        public LookupIndexWriter(
            IKeyExtractor<TValue> indexedKeyExtractor,
            Func<TValue, string> indexedValueExtractor,
            TimeSpan? expiry,
            Func<string, string> indexCollectionNameGenerator)
            : base(indexedKeyExtractor, indexedValueExtractor, expiry)
        {
            _collectionNameGenerator = indexCollectionNameGenerator;
        }

        private string GenerateSetName(string indexedKey) => _collectionNameGenerator(indexedKey);

        public override void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem)
        {
            var oldKey = oldItem != null ? IndexedKeyExtractor.ExtractKey(oldItem) : null;
            var newKey = newItem != null ? IndexedKeyExtractor.ExtractKey(newItem) : null;

            if (oldKey != null && !oldKey.Equals(newKey, StringComparison.OrdinalIgnoreCase))
                Remove(context, new[] {oldItem});

            if (newKey != null)
                Set(context, new[] {newItem});
        }

        public override void Remove(IDatabaseAsync context, IEnumerable<TValue> items)
        {
            foreach (var item in items)
                context.SetRemoveAsync(GenerateSetName(IndexedKeyExtractor.ExtractKey(item)),
                    IndexedValueExtractor(item));
        }

        public override void Set(IDatabaseAsync context, IEnumerable<TValue> items)
        {
            var indexEntries = items.ToSets(IndexedKeyExtractor.ExtractKey, IndexedValueExtractor).ToArray();
            foreach (var entry in indexEntries)
            {
                context.SetAddAsync(GenerateSetName(entry.Key), entry.ToArray());
                context.KeyExpireAsync(entry.Key, Expiry);
            }
        }
    }
}