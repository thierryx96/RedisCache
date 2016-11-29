using System.Collections.Generic;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadIndexedStoreAsync<TValue>
    {
        Task<TValue[]> GetItemsByIndexAsync<TValueExtractor>(string indexedKey)
            where TValueExtractor : IKeyExtractor<TValue>;

        Task<IDictionary<string, TValue[]>> GetItemsByIndexAsync<TValueExtractor>(IEnumerable<string> indexedKeys)
            where TValueExtractor : IKeyExtractor<TValue>;

        Task<string[]> GetMasterKeysByIndexAsync<TValueExtractor>(string indexedKey)
            where TValueExtractor : IKeyExtractor<TValue>;

        Task<IDictionary<string, string[]>> GetMasterKeysByIndexAsync<TValueExtractor>(IEnumerable<string> indexedKeys)
            where TValueExtractor : IKeyExtractor<TValue>;
    }
}