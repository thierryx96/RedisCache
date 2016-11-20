using System.Collections.Generic;

namespace PEL.Framework.Redis.Store.Indexed
{
    public interface IRedisIndexedStore<out TValue>
    {

        IEnumerable<string> GetKeysByIndexAsync(string value);

       IEnumerable<TValue> GetValuesByIndexAsync(string value);

    }

}