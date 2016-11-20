using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal interface IMasterValueResolver<TValue>
    {
        Task<IEnumerable<TValue>> GetMasterValues(IDatabaseAsync context, string value);
        Task<IEnumerable<TValue>> GetAllMasterValues(IDatabaseAsync context);

    }
}