using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal interface IMasterValueResolverAsync<TValue>
    {
        /// <summary>
        ///     Get the indexed values that match the indexed key
        /// </summary>
        Task<TValue[]> GetMasterValuesAsync(IDatabaseAsync context, string indexedKey);

        /// <summary>
        ///     Get all the matched indexed values for the indexed keys
        ///     ...
        ///     indexedKey1 -> [values matching key 1]
        ///     indexedKey2 -> [values matching key 2]
        ///     ...
        /// </summary>
        Task<IDictionary<string, TValue[]>> GetMasterValuesAsync(IDatabaseAsync context, IEnumerable<string> indexedKeys);
    }
}