using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal interface IMasterValueResolver<TValue>
    {
        TValue[] GetMasterValues(IDatabase context, string indexedKey);

        IDictionary<string, TValue[]> GetMasterValues(IDatabase context, IEnumerable<string> indexedKeys);
    }
}