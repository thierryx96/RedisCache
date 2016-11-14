using System.Collections.Generic;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisReadStore<out TValue>
    {
        TValue Get(string key);

        IEnumerable<TValue> GetAll();
    }
}