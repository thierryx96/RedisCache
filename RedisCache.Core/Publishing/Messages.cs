using System.Collections.Generic;

namespace PEL.ES.Infrastructure.Caching.Publishing
{
    public class ItemAddedMessage<TData>
    {
        public string Id { get; set; }
        public TData Item { get; set; }
    }

    public class ItemUpdatedMessage<TData>
    {
        public string Id { get; set; }
        public TData Item { get; set; }
    }

    public class ItemDeletedMessage<TData>
    {
        public string Id { get; set; }
    }

    public class InitialLoadNeededMessage<TData>
    {
        public IDictionary<string, TData> Items { get; set; }
    }
}