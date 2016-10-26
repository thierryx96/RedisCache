using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console
{
    class Program
    {
        class Thing
        {
            public string Text { get; set; }
            public int Number { get; set; }
        }

        class Data
        {
            public Data(string name)
            {
                Name = name;
                ListOfStrings = Enumerable.Range(0, 5).Select(i => $"str{i}").ToList();
                ListOfThings = Enumerable.Range(0, 5).Select(i => new Thing() { Text = $"str{i}", Number = i }).ToList();
                Thing = new Thing() { Text = "thingy", Number = 0 };
            }

            public string Name { get; set; }
            public List<string> ListOfStrings { get; set; }
            public List<Thing> ListOfThings { get; set; }
            public Thing Thing { get; set; }
        }

        static HashEntry[] GenerateRedisHash<T>(T obj)
        {
            var props = typeof(T).GetProperties();
            var hash = new HashEntry[props.Count()];
            for (int i = 0; i < props.Count(); i++)
                hash[i] = new HashEntry(props[i].Name, props[i].GetValue(obj).ToString());
            return hash;
        }

        static void Main(string[] args)
        {

            const int keysCount = 100;
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            IDatabase db = redis.GetDatabase();

            
            ConsoleKey mainKey = ConsoleKey.End;
            while (mainKey != ConsoleKey.Escape)
            {
                if (mainKey >= ConsoleKey.D0 && mainKey <= ConsoleKey.D9)
                {
                    var key = mainKey.ToString()[1];
                    var value = db.StringGet($"cs:{key}");

                    //var data = db.HashGetAll($"cs:{key}");
                    System.Console.Out.WriteLine(value);
                }
                else
                {
                    switch (mainKey)
                    {
                        case ConsoleKey.F1:
                            {
                                for (int i = 0; i < keysCount; i++)
                                {
                                    // GenerateRedisHash(new Data($"Thingy#{i}"))
                                    db.StringSet($"cs:{i}", JObject.FromObject(new Data($"Thingy#{i}")).ToString());
                                }
                            }
                            break;

                        case ConsoleKey.F2:
                            {
                            }
                            break;
                        case ConsoleKey.F3:
                            {
                            }
                            break;
                        case ConsoleKey.Escape:
                            {
                                Environment.Exit(0);
                            }

                            break;

                            

                        case ConsoleKey.M:
                            {
                                
                            }
                            break;

                        case ConsoleKey.R:
                            break;


                        case ConsoleKey.F11:
                            //scheduler.CleanIndex();
                            //var mkts = store.GetAllMarkets();
                            //var mkt = mkts.Where(m => m.Start > DateTimeOffset.UtcNow).First();
                            //Console.WriteLine("Import Selections 1 market:" + mkt);

                            //scheduler.ImportSelectionsForMarket(mkt.Id);
                            //Console.WriteLine("Imported Selections for market");

                            break;
                        case ConsoleKey.F12:
                            break;
                        case ConsoleKey.F13:
                            break;
                        case ConsoleKey.F14:
                            break;
                        case ConsoleKey.F15:
                            break;
                        case ConsoleKey.Zoom:
                            break;
                        default:
                            break;
                    }
                }
                mainKey = System.Console.ReadKey().Key;
            }
        }
    }
    
}
