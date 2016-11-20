using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public interface IIndex<TValue> : IMasterKeyResolver
    {
        string Name { get; }

        IKeyExtractor<TValue> Extractor { get; set; }

        void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
        void Set(IDatabaseAsync context, IEnumerable<TValue> items);
        void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
        void Clear(IDatabaseAsync context);
    }





}
