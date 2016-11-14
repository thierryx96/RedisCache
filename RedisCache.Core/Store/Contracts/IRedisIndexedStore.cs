using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Store
{
    public interface IRedisIndexedStore<out TValue>
    {
        IEnumerable<string> GetMasterKeysByIndex(string indexName, string value);
        IEnumerable<TValue> GetItemsByIndex(string indexName, string value);
    }
}
