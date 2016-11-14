namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisExpirableStore<TValue> :
        IRedisWriteStoreAsync<TValue>,
        IRedisReadStore<TValue>,
        IRedisReadStoreAsync<TValue>
    {
        string ExtractMasterKey(TValue value);
    }
}