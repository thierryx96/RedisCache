using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache
{
    public class IndexDefinition<TValue>
    {
        public static IndexDefinition<TValue> CreateUniqueFromExtractor(string name, Func<TValue, string> extractor)
        {
            return new IndexDefinition<TValue>()
            {
                KeyExtractor = extractor,
                Unique = true,
                Name = name
            };
        }

        public static IndexDefinition<TValue> CreateNonUniqueFromExtractor(string name, Func<TValue, string> extractor)
        {
            return new IndexDefinition<TValue>()
            {
                KeyExtractor = extractor,
                Unique = false,
                Name = name
            };
        }

        public string Name { get; set; }
        public bool Unique { get; set; }
        public Func<TValue, string> KeyExtractor { get; set; }
    }
}
