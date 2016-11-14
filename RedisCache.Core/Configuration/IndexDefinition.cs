using System;

namespace PEL.Framework.Redis.Configuration
{
    public class IndexDefinition<TValue>
    {
        public string Name { get; set; }
        public bool Unique { get; set; }
        public Func<TValue, string> KeyExtractor { get; set; }
    }
}