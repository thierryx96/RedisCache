using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing.Writers;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{

      internal interface IIndex<TValue> : IMasterKeyResolverAsync, IMasterValueResolverAsync<TValue>
        {
          //string Name { get; }
          IKeyExtractor<TValue> Extractor { get; set; }
          void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
          void Set(IDatabaseAsync context, IEnumerable<TValue> items);
          void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
          
    }
}
