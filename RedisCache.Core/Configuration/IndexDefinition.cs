using System;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing;

namespace PEL.Framework.Redis.Configuration
{
    public class IndexSettings<TValue>
    {
        public bool Unique { get; set; }

        public IKeyExtractor<TValue> Extractor { get; set; }

        public string Name { get; set; }

    }
}