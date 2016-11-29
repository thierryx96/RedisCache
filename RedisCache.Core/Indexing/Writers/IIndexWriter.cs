using System.Collections.Generic;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Writers
{
    internal interface IIndexWriter<TValue> 
    {
        IKeyExtractor<TValue> IndexedKeyExtractor { get; }
        void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
        void Set(IDatabaseAsync context, IEnumerable<TValue> items);
        void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
    }
}