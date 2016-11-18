using System.Collections.Generic;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Store
{
    public interface IRedisReadIndexedStoreAsync<TValue>
    {
        Task<IEnumerable<string>> GetKeysByIndexAsync<TExtractor>(string value)
            where TExtractor : IKeyExtractor<TValue>;

        Task<IEnumerable<TValue>> GetItemsByIndexAsync<TExtractor>(string value)
            where TExtractor : IKeyExtractor<TValue>;

    }
}