using System.Collections.Generic;
using System.Threading.Tasks;

namespace PEL.ES.Infrastructure.Caching.Publishing
{
    public interface IDataChangeEventPublisher
    {
        Task PublishAdded<TData>(string key, TData addedItem);
        Task PublishDeleted<TData>(string key);
        Task PublishUpdated<TData>(string key, TData updatedItem);
        Task PublishInitLoadNeeded<TData>(IDictionary<string, TData> items);
    }
}