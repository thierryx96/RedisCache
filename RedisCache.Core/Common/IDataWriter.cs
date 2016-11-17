using System.Collections.Generic;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Common
{
    internal interface IDataWriter<in TValue>
    {
        void Remove(IDatabaseAsync context, IEnumerable<TValue> items);
        void Set(IDatabaseAsync context, IEnumerable<TValue> items);
        void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem);
        void Clear(IDatabaseAsync context);
    }
}
