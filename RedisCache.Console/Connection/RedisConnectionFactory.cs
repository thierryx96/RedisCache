using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console
{
    public class RedisConnectionFactory
    {
        private static readonly Lazy<ConnectionMultiplexer> Connection;
        static RedisConnectionFactory()
        {
            var connectionString =
                ConfigurationManager.AppSettings["redis:connection"].ToString();
            var options = ConfigurationOptions.Parse(connectionString);
            Connection = new Lazy<ConnectionMultiplexer>(
                () => ConnectionMultiplexer.Connect(options)
            );
        }
        public static ConnectionMultiplexer GetConnection() => Connection.Value;
    }
}
