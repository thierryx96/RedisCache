using System;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Configuration
{
    public class CollectionSettings<TValue>
    {
        /// <summary>
        /// Extractor for the master primary unique key of the collection
        /// </summary>
        public IKeyExtractor<TValue> MasterKeyExtractor { get; set; }

        /// <summary>
        /// Redis internal name for the collection, if let redis will use the type of the collection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Time to live (TTL) for the collection, a collection expires (= is deleted) after this time.
        /// </summary>
        public TimeSpan? Expiry { get; set; }
    }
}