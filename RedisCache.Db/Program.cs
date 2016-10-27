using RedisCache.Console;
using RedisCache.Console.Messaging;
using RedisCache.Console.Models;
using RedisCache.Console.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Db
{
    class Program
    {
        static void Main(string[] args)
        {

                const int keysCount = 100;
            //ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            //IDatabase db = redis.GetDatabase();
            // RedisCacheStore cache = new Console.RedisCacheStore(new Console.Serialization.JsonSerializer());
            var connection = RedisConnectionFactory.GetConnection();
            var serializer = new JsonSerializer();
           // RedisCacheStore cache = new RedisCacheStore(new Console.Serialization.JsonSerializer(), connectionMultiplexer);
            ISubscriber sub = connection.GetSubscriber();


            ConsoleKey mainKey = ConsoleKey.End;
                while (mainKey != ConsoleKey.Escape)
                {
                    if (mainKey >= ConsoleKey.D0 && mainKey <= ConsoleKey.D9)
                    {
                    var key = $"cs:{mainKey.ToString()[1]}";
                    var data = new DataEntity(key);
                    var payload = new UpdateMessage<DataEntity>()
                    {
                        Key = key,
                        Data = data
                    };

                    sub.Publish("cs:update", serializer.Serialize(payload));
                    System.Console.Out.WriteLine($"Published:"+key);
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
