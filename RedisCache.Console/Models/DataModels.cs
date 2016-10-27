using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console.Models
{
    public class Thing
    {
        public string Text { get; set; }
        public int Number { get; set; }
    }

    public class DataEntity
    {
        public DataEntity(string name)
        {
            Name = name;
            //ListOfStrings = Enumerable.Range(0, 5).Select(i => $"str{i}").ToList();
            //ListOfThings = Enumerable.Range(0, 5).Select(i => new Thing() { Text = $"str{i}", Number = i }).ToList();
            Thing = new Thing() { Text = "thingy", Number = 0 };
            UpdatedAt = DateTime.Now;
        }

        public DateTime UpdatedAt { get; set; }
        public string Name { get; set; }
        //public List<string> ListOfStrings { get; set; }
        //public List<Thing> ListOfThings { get; set; }
        public Thing Thing { get; set; }
    }
}
