using System.Collections.Generic;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisIndexedStore<out TValue>
    {

        IEnumerable<string> GetKeysByIndexAsync(string value);

       IEnumerable<TValue> GetValuesByIndexAsync(string value);

    }

}