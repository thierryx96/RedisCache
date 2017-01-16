using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public interface IMasterKeyResolverAsync
    {
        Task<string[]> GetMasterKeysAsync(IDatabaseAsync context, string indexedKey);

        /// <summary>
        ///     Get all the matched indexed values for the indexed keys
        ///     ...
        ///     indexedKey1 -> [values matching key 1]
        ///     indexedKey2 -> [values matching key 2]
        ///     ...
        /// </summary>
        Task<IDictionary<string, string[]>> GetMasterKeysAsync(IDatabaseAsync context, IEnumerable<string> indexedKeys);
    }
}