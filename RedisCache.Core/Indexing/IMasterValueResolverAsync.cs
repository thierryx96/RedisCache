using System.Threading.Tasks;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal interface IMasterValueResolverAsync<TValue>
    {
        Task<TValue[]> GetMasterValuesAsync(IDatabaseAsync context, string indexedKey);
        Task<TValue[]> GetAllMasterValuesAsync(IDatabaseAsync context);
    }
}