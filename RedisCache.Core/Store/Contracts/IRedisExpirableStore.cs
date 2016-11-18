namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisExpirableStore<TValue> :
        IRedisWriteStoreAsync<TValue>,
        IRedisWriteStore<TValue>,
        IRedisReadStore<TValue>,
        IRedisReadStoreAsync<TValue>
    {
        string ExtractMasterKey(TValue value);

    }


}