using System.Collections.Generic;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Store.Contracts
{
    public interface IRedisWriteStoreAsync<in TValue>
    {
        Task ClearAsync();

        Task SetAsync(IEnumerable<TValue> items);

        Task AddOrUpdateAsync(TValue item);

        Task RemoveAsync(string masterKey);

        Task RemoveAsync(IEnumerable<string> masterKeys);
    }
}