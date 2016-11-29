using System;
using System.Collections.Generic;
using System.Linq;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Writers
{

    internal class LookupIndexWriter<TValue> : IIndexWriter<TValue>
    {
        private readonly string _indexCollectionPrefix;
        public IKeyExtractor<TValue> IndexedKeyExtractor { get; private set; }
        TimeSpan? Expiry { get; }
        private Func<TValue, string> IndexedValueExtractor { get; set; }
        private string GenerateSetName(string indexedKey) => $"{_indexCollectionPrefix}[{indexedKey}]";

        public LookupIndexWriter(
            IKeyExtractor<TValue> indexedKeyExtractor,
            Func<TValue, string> indexedValueExtractor,
            string indexCollectionPrefix,
            TimeSpan? expiry)
        {
            IndexedValueExtractor = indexedValueExtractor;
            IndexedKeyExtractor = indexedKeyExtractor;
            _indexCollectionPrefix = indexCollectionPrefix;
            Expiry = expiry;
        }


        public void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem)
        {
            var oldKey = oldItem != null ? IndexedKeyExtractor.ExtractKey(oldItem) : null;
            var newKey = newItem != null ? IndexedKeyExtractor.ExtractKey(newItem) : null;

            if (oldKey != null && !oldKey.Equals(newKey, StringComparison.OrdinalIgnoreCase))
            {
                Remove(context, new[] { oldItem });
            }

            if (newKey != null)
            {
                Set(context, new[] { newItem });
            }
        }

        public void Remove(IDatabaseAsync context, IEnumerable<TValue> items)
        {
            foreach (var item in items)
            {
                context.SetRemoveAsync(GenerateSetName(IndexedKeyExtractor.ExtractKey(item)), IndexedValueExtractor(item));
            }
        }

        public void Set(IDatabaseAsync context, IEnumerable<TValue> items)
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
