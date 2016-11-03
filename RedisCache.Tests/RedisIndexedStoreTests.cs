using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StackExchange.Redis;
using RedisCache.Store;
using RedisCache.Serialization;

namespace RedisCache.Tests
{
    

    [Explicit]
    [TestFixture]
    internal class RedisIndexedStoreTests 
    {
        //private RedisIndexedStore<TestType1Entity> _cacheType1;
        //private RedisIndexedStore<TestType2Entity> _cacheType2;
        //private RedisIndexedStore<TestType1Entity> _cacheType1WithExpiry;

        private ConnectionMultiplexer _connection;
        private const string RedisConnectionOptions = "localhost:6379,allowAdmin=true"; //TODO(thierryr): move to config file (if we are using a specific config for CI env. or Integration Tests)

        private RedisIndexedStore<TestType1Entity> _cacheType1;
        private RedisIndexedStore<TestType2Entity> _cacheType2;
        private RedisIndexedStore<TestType1Entity> _cacheType1WithExpiry;

        [OneTimeSetUp]
        public  void OneTimeSetUp()
        {
            _connection = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));
            _cacheType1 = new RedisIndexedStore<TestType1Entity>(_connection, new JsonSerializer(), entity => entity.Id, new Dictionary<string, Func<TestType1Entity, string>>() { { "name", e => e.Name } }, null);
            _cacheType2 = new RedisIndexedStore<TestType2Entity>(_connection, new JsonSerializer(), entity => entity.Id, new Dictionary<string, Func<TestType2Entity, string>>() { { "name", e => e.Name } }, null);
            _cacheType1WithExpiry = new RedisIndexedStore<TestType1Entity>(_connection, new JsonSerializer(), entity => entity.Id, new Dictionary<string, Func<TestType1Entity, string>>() { { "name", e => e.Name } }, TimeSpan.FromSeconds(1));
        }

        private static readonly List<TestType1Entity> TestType1Entities = Enumerable.Range(0, 10).Select(i => new TestType1Entity { Name = $"testType1#{i}", Id = $"id#{i}" }).ToList();

        [SetUp]
        public async Task SetUp()
        {
            await _cacheType1.Flush();
            await _cacheType2.Flush();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _cacheType1.Flush();
            await _cacheType2.Flush();
            _connection?.Dispose();
        }

        [Test]
        public async Task AddOrUpdate_WhenAnEntityIsAdded_ThenItCanBeRetrieved()
        {
            // arrange
            var entity = new TestType1Entity { Name = "test", Id = "1" };

            // act
            await _cacheType1.AddOrUpdate(entity);
            var retrievedEntity = await _cacheType1.GetValueByIndex("name",entity.Name);
            var retrievedEntityId = await _cacheType1.GetKeyByIndex("name",entity.Name);

            // assert
            Assert.That(retrievedEntity.Id, Is.EqualTo(entity.Id));
            Assert.That(retrievedEntity.Name, Is.EqualTo(entity.Name));
        }

        [Test]
        public async Task AddOrUpdate_WhenAnEntityIsAddedWithExpiryOf1Second_ThenItCannotBeRetrieved()
        {
            // arrange
            var entity = new TestType1Entity { Name = "test", Id = "1" };

            // act
            await _cacheType1WithExpiry.AddOrUpdate(entity);
            var retrievedEntityBeforeExpiry = await _cacheType1.GetValueByIndex("name", entity.Name);

            await Task.Delay(1500);
            var retrievedEntityAfterExpiry = await _cacheType1.GetValueByIndex("name", entity.Name);
            var retrievedEntityIdAfterExpiry = await _cacheType1.GetKeyByIndex("name", entity.Name);

            // assert
            Assert.That(retrievedEntityBeforeExpiry.Name, Is.EqualTo(entity.Name));
            Assert.That(retrievedEntityBeforeExpiry.Id, Is.EqualTo(entity.Id));

            Assert.That(retrievedEntityAfterExpiry, Is.Null);
            Assert.That(retrievedEntityIdAfterExpiry, Is.Null);

        }
    }
}