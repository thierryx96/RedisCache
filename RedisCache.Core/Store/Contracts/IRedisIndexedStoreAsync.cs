using System.Collections.Generic;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Store
{
    public interface IRedisIndexedStoreAsync<TValue>
    {
        Task<IEnumerable<string>> GetAllKeysByIndexAsync(string indexName, string value);
        Task<IEnumerable<TValue>> GetAllByIndexAsync(string indexName, string value);
    }
}