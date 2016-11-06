using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache
{
    public interface IRedisWriteStore<TValue>
    {
        Task Flush();

        Task Set(IEnumerable<TValue> items);

        Task AddOrUpdate(TValue item);

        Task Remove(string key);
    }

    public interface IRedisReadStore<TValue>
    {
        Task<TValue> Get(string key);

        Task<IEnumerable<TValue>> GetAll();
    }

    public interface IRedisStore<TValue>
    {
        Func<TValue, string> MasterKeyExtractor { get; }
        string CollectionRootName { get; }
        TimeSpan? Expiry { get; }
    }
}
