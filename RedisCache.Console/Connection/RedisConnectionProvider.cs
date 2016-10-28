using System;
using System.Configuration;
using StackExchange.Redis;

namespace RedisCache.Console.Connection
{
    // TODO : startegy do define a scoped singleton for this class on autofac
    /// <summary>
    /// connection to the Azure Redis Cache is managed by the ConnectionMultiplexer class. 
    /// This class is designed to be shared and reused throughout your client application, and must not be created on a per operation basis
    /// </summary>
    public class RedisConnectionProvider
    {
        private static readonly Lazy<ConnectionMultiplexer> Connection;
        static RedisConnectionProvider()
        {
            var connectionString =  ConfigurationManager.AppSettings["Redis.ConnectionOptions"];
            var options = ConfigurationOptions.Parse(connectionString);
            Connection = new Lazy<ConnectionMultiplexer>(
                () => ConnectionMultiplexer.Connect(options)
            );
        }
        public static ConnectionMultiplexer GetConnection() => Connection.Value;
    }
}
