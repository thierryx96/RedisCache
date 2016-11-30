using System;
using System.Collections.Generic;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Writers
{
    internal abstract class IndexWriter<TValue> 
    {
        protected IKeyExtractor<TValue> IndexedKeyExtractor { get; private set; }
        protected TimeSpan? Expiry { get; }
        protected Func<TValue, string> IndexedValueExtractor { get; set; }

        protected IndexWriter(
            IKeyExtractor<TValue> indexedKeyExtractor,
            Func<TValue, string> indexedValueExtractor,
            TimeSpan? expiry)
        {
            IndexedValueExtractor = indexedValueExtractor;
            IndexedKeyExtractor = indexedKeyExtractor;
            Expiry = expiry;
        }

        public abstract void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
        public abstract void Set(IDatabaseAsync context, IEnumerable<TValue> items);
        public abstract void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
    }
}