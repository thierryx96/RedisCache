using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Indexing
{

    public interface IKeyResolver
    {
        Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string value);
    }
        

    public interface IIndex<TValue> : IKeyResolver
    {
        string Name { get;  }
        Func<TValue, string> KeyExtractor { get;  }

        void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
        void Set(IDatabaseAsync context, IEnumerable<TValue> items);
        void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
        void Flush(IDatabaseAsync context);
    }
}
