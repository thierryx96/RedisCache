using System;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Configuration
{
    public class CollectionSettings<TValue>
    {
        public IKeyExtractor<TValue> MasterKeyExtractor { get; set; }

        public string Name { get; set; }

        public TimeSpan? Expiry { get; set; }
    }
}