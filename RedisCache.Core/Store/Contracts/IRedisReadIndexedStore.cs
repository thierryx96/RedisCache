using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadIndexedStore<out TValue>
    {
        string[] GetMasterKeysByIndex<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>;

        TValue[] GetItemsByIndex<TValueExtractor>(string value)
            where TValueExtractor : IKeyExtractor<TValue>;
    }
}