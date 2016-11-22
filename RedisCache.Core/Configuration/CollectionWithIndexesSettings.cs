using System.Collections.Generic;

namespace PEL.Framework.Redis.Configuration
{
    public class CollectionWithIndexesSettings<TValue> : CollectionSettings<TValue>
    {
        /// <summary>
        /// List of indexes for this collections
        /// </summary>
        public IEnumerable<IndexSettings<TValue>> Indexes { get; set; }
    }
}