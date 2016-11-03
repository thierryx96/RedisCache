using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StackExchange.Redis;
using RedisCache.Store;
using RedisCache.Serialization;
using RedisCache.Tests;

namespace RedisCache.Tests
{
    

    [Explicit]
    [TestFixture]
    internal class RedisIndexedStoreDerivedTests : RedisCacheStoreTests
    {


        private ConnectionMultiplexer _connection;
        private const string RedisConnectionOptions = "localhost:6379,allowAdmin=true"; //TODO(thierryr): move to config file (if we are using a specific config for CI env. or Integration Tests)

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            _connection = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));
            _cacheType1 = new RedisIndexedStore<TestType1Entity>(_connection, new JsonSerializer(), entity => entity.Id, new Dictionary<string, Func<TestType1Entity, string>>(){ { "name", e => e.Name } }, null);
            _cacheType2 = new RedisIndexedStore<TestType2Entity>(_connection, new JsonSerializer(), entity => entity.Id, new Dictionary<string, Func<TestType2Entity, string>>() { { "name", e => e.Name } },null);
            _cacheType1WithExpiry = new RedisIndexedStore<TestType1Entity>(_connection, new JsonSerializer(), entity => entity.Id, new Dictionary<string, Func<TestType1Entity, string>>() { { "name", e => e.Name } }, TimeSpan.FromSeconds(1));
        }
    }
}