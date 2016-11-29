using System.Collections.Generic;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadStoreAsync<TValue>
    {
        /// <summary>
        /// Get one single item from the collection
        /// </summary>
        Task<TValue> GetAsync(string masterKey);

        /// <summary>
        /// Get many items from the collection, filtered by keys
        /// </summary>
        Task<TValue[]> GetAsync(IEnumerable<string> masterKeys);

        /// <summary>
        /// Get all items in the collection
        /// </summary>
        /// <returns></returns>
        Task<TValue[]> GetAllAsync();
    }
}