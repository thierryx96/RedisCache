using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Tests
{
    public class ItemType
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class TestType1Entity
    {
        public TestType1Entity()
        {
            Dictionary = new Dictionary<string, ItemType>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public ItemType NestedProperty { get; set; }

        public IDictionary<string, ItemType> Dictionary { get; set; }
    }

    public class TestType2Entity
    {
        public TestType2Entity()
        {
            Dictionary = new Dictionary<string, ItemType>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public ItemType NestedProperty { get; set; }

        public IDictionary<string, ItemType> Dictionary { get; set; }
    }


}
