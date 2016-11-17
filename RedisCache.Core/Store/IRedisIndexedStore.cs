using System.Collections.Generic;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Store
{
    public interface IRedisIndexedStore<TValue>
    {
        Task<IEnumerable<string>> GetKeysByIndex(string indexName, string value);
        Task<IEnumerable<TValue>> GetItemsByIndex(string indexName, string value);
    }
}
