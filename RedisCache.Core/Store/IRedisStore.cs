using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisCache.Store
{
    public interface IRedisWriteStore<in TValue>
    {
        Task Clear();

        Task Set(IEnumerable<TValue> items);

        Task AddOrUpdate(TValue item);

        Task Remove(params string[] keys);

    }

    public interface IRedisReadStore<TValue>
    {
        Task<TValue> Get(string key);

        Task<IEnumerable<TValue>> GetAll();
    }

    public interface IRedisStore<in TValue>
    {
        Func<TValue, string> MasterKeyExtractor { get; }
        string CollectionRootName { get; }
        TimeSpan? Expiry { get; }
    }
}
