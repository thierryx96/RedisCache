using System.Collections.Generic;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadStore<out TValue>
    {
        /// <summary>
        /// Get one single item from the collection
        /// </summary>
        TValue Get(string masterKey);

        /// <summary>
        /// Get many items from the collection, filtered by keys
        /// </summary>
        TValue[] Get(IEnumerable<string> masterKeys);

        /// <summary>
        /// Get all items in the collection
        /// </summary>
        /// <returns></returns>
        TValue[] GetAll();
    }
}