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
    }
}