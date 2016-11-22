namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisExpirableIndexedStore<TValue> :
        IRedisExpirableStore<TValue>,
        IRedisReadIndexedStoreAsync<TValue>,
        IRedisReadIndexedStore<TValue>
    {
    }
}