using System;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Configuration
{
    public class IndexSettings<TValue>
    {
        public bool Unique { get; set; }

        public bool WithPayload { get; set; }

        public IKeyExtractor<TValue> Extractor { get; set; }

        public string Name { get; set; }
    }
}