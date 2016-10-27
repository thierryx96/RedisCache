using Newtonsoft.Json.Linq;
using RedisCache.Console.Messaging;
using RedisCache.Console.Models;
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
            var connectionMultiplexer = RedisConnectionFactory.GetConnection();
            var serializer = new Console.Serialization.JsonSerializer();
            RedisCacheStore cache = new RedisCacheStore(serializer, connectionMultiplexer);
            var sub = connectionMultiplexer.GetSubscriber();
            
            sub.Subscribe("cs:update", (channel, message) => {
                System.Console.Out.WriteLine("received:"+(string)message);
                var payload = serializer.Deserialize<UpdateMessage<DataEntity>>((string)message);
                cache.Set<DataEntity>(payload.Key, payload.Data, TimeSpan.FromHours(1));
            });

            ConsoleKey mainKey = ConsoleKey.End;
            while (mainKey != ConsoleKey.Escape)
            {
                if (mainKey >= ConsoleKey.D0 && mainKey <= ConsoleKey.D9)
                {
                    var key = mainKey.ToString()[1];
                    var value = cache.Get<DataEntity>($"cs:{key}");

                    //var data = db.HashGetAll($"cs:{key}");
                    System.Console.Out.WriteLine("read:"+serializer.Serialize(value));
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
                                    cache.Set<DataEntity>($"cs:{i}", new DataEntity($"Thingy#{i}"), TimeSpan.FromHours(1));
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
