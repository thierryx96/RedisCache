using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal interface IMasterKeyResolver
    {
        Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string value);
    }

}
