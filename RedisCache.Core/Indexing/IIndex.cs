using System.Collections.Generic;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal interface IIndex<TValue> : IMasterKeyResolverAsync, IMasterValueResolverAsync<TValue>
    {
        IKeyExtractor<TValue> Extractor { get; set; }
        void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
        void Set(IDatabaseAsync context, IEnumerable<TValue> items);
        void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
    }
}