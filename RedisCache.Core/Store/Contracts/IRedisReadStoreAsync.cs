using System.Collections.Generic;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadStoreAsync<TValue>
    {
        Task<TValue> GetAsync(string key);

        Task<IEnumerable<TValue>> GetAllAsync();
    }
}