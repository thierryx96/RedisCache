using System.Collections.Generic;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadStoreAsync<TValue>
    {
        /// <summary>
        /// Get one single item from the collection
        /// </summary>
        Task<TValue> GetAsync(string key);

        /// <summary>
        /// Get many items from the collection, filtered by keys
        /// </summary>
        Task<TValue[]> GetAsync(IEnumerable<string> keys);

        /// <summary>
        /// Get all items in the collection
        /// </summary>
        /// <returns></returns>
        Task<TValue[]> GetAllAsync();
    }
}