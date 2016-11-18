using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public interface IIndex<TValue>
    {
        string Name { get; }

        IKeyExtractor<TValue> Extractor { get; set; }

        Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string value);
        void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
        void Set(IDatabaseAsync context, IEnumerable<TValue> items);
        void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
        void Clear(IDatabaseAsync context);
    }

    public interface IIndexWithValues<TValue>
    {
        string Name { get; }
        IKeyExtractor<TValue> Extractor { get; set; }
        Task<IEnumerable<TValue>> GetMasterValues(IDatabaseAsync context, string value);
        void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
        void Set(IDatabaseAsync context, IEnumerable<TValue> items);
        void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
        void Clear(IDatabaseAsync context);
    }



}
