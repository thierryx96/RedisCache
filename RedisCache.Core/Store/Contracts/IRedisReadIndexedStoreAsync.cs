using System.Collections.Generic;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadIndexedStoreAsync<TValue>
    {
        Task<string[]> GetMasterKeysByIndexAsync<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>;

        Task<TValue[]> GetItemsByIndexAsync<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>;

        Task<IDictionary<string, string[]>> GetMasterKeysByIndexAsync<TValueExtractor>(IEnumerable<string> values)
            where TValueExtractor : IKeyExtractor<TValue>;

        Task<IDictionary<string, TValue[]>> GetItemsByIndexAsync<TValueExtractor>(IEnumerable<string> values)
            where TValueExtractor : IKeyExtractor<TValue>;
    }
}