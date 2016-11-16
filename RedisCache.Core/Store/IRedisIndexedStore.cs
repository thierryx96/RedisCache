using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisCache.Store
{
    public interface IRedisIndexedStore<TValue>
    {
        Task<IEnumerable<string>> GetKeysByIndex(string indexName, string value);
        Task<IEnumerable<TValue>> GetItemsByIndex(string indexName, string value);
    }
}
