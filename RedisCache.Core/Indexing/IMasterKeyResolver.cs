using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public interface IMasterKeyResolver
    {
        Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string value);
        Task<IEnumerable<string>> GetAllMasterKeys(IDatabaseAsync context);

    }

}
