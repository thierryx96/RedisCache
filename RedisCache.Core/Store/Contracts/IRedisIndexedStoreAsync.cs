using System.Collections.Generic;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisIndexedStoreAsync<TValue>
    {
        Task<IEnumerable<string>> GetMasterKeysByIndexAsync<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>;

        Task<IEnumerable<TValue>> GetItemsByIndexAsync<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>;

    }
}
