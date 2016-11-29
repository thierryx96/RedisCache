using System.Collections.Generic;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadIndexedStore<TValue>
    {
        string[] GetMasterKeysByIndex<TValueExtractor>(string indexedKey)
            where TValueExtractor : IKeyExtractor<TValue>;

        TValue[] GetItemsByIndex<TValueExtractor>(string indexedKey)
            where TValueExtractor : IKeyExtractor<TValue>;

        IDictionary<string, TValue[]> GetItemsByIndex<TValueExtractor>(IEnumerable<string> indexedKeys)
            where TValueExtractor : IKeyExtractor<TValue>;

        IDictionary<string, string[]> GetMasterKeysByIndex<TValueExtractor>(IEnumerable<string> indexedKeys)
            where TValueExtractor : IKeyExtractor<TValue>;
    }
}