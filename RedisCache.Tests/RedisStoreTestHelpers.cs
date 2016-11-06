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

    public class TestType1Entity : IEquatable<TestType1Entity>
    {
        public TestType1Entity()
        {

        }

        public TestType1Entity(string id, string name, string category)
        {
            Id = id;
            Name = name;
            NestedProperty = new ItemType();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public ItemType NestedProperty { get; set; }

        public bool Equals(TestType1Entity other)
        {
            return Id == other.Id && Name == other.Name && Category == other.Category;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Id.GetHashCode() + Name.GetHashCode() + Category.GetHashCode();
            }
            
        }
    }

    public class TestType2Entity
    {
        public TestType2Entity()
        {
            //Dictionary = new Dictionary<string, ItemType>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public ItemType NestedProperty { get; set; }

    }


}
