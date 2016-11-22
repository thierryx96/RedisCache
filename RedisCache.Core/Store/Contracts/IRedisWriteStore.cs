using System.Collections.Generic;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisWriteStore<in TValue>
    {
        void AddOrUpdate(TValue item);
        void Set(IEnumerable<TValue> items);
    }
}