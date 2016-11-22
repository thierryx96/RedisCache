namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadStore<out TValue>
    {
        TValue Get(string key);

        TValue[] GetAll();
    }
}