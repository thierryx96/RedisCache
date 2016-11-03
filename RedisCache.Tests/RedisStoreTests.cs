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
    internal class RedisCacheStoreTests
    {
        protected RedisCacheStore<TestType1Entity> _cacheType1;
        protected RedisCacheStore<TestType2Entity> _cacheType2;
        protected RedisCacheStore<TestType1Entity> _cacheType1WithExpiry;

        private ConnectionMultiplexer _connection;
        private const string RedisConnectionOptions = "localhost:6379,allowAdmin=true"; //TODO(thierryr): move to config file (if we are using a specific config for CI env. or Integration Tests)

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            _connection = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));
            _cacheType1 = new RedisCacheStore<TestType1Entity>(_connection, new JsonSerializer(), entity=> entity.Id, null);
            _cacheType2 = new RedisCacheStore<TestType2Entity>(_connection, new JsonSerializer(), entity => entity.Id, null);
            _cacheType1WithExpiry = new RedisCacheStore<TestType1Entity>(_connection, new JsonSerializer(), entity => entity.Id, TimeSpan.FromSeconds(1));

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
        public async Task Set_WhenManyEntitiesAreLoadedThenOneAdded_ThenAllCanBeRetrieved()
        {
            //TODO(thierryr): we doing a bit much here, split in many tests later

            // arrange
            var firstLoad = TestType1Entities.Take(5).ToArray();
            var lastLoad = TestType1Entities.Skip(5).ToArray();
            var expected = firstLoad.Union(lastLoad).ToArray();

            // act
            await _cacheType1.Set(firstLoad);

            // assert
            var retrievedFirstLoad = (await _cacheType1.GetAll()).ToArray();
            Assert.That(retrievedFirstLoad.Select(v => v.Name), Is.EquivalentTo(firstLoad.Select(v => v.Name)));

            // act
            foreach (var entity in lastLoad)
            {
                var key = entity.Id;
                await _cacheType1.AddOrUpdate(entity);
            }

            var retrievedAllEntities = (await _cacheType1.GetAll()).ToArray();

            // assert
            Assert.That(retrievedAllEntities.Select(v => v.Name), Is.EquivalentTo(expected.Select(v => v.Name)));
        }

        [Test]
        public async Task Set_GivenEmptyStore_WhenItemsAddedAreEmpty_ThenTheKeyMustBeFoundAndContainsNoItems()
        {
            // arrange
            var emptyCollection = new TestType1Entity[] { };

            // act
            await _cacheType1.Set(emptyCollection);

            var allKeys = _connection.GetEndPoints().SelectMany(endPoint => _connection.GetServer(endPoint).Keys().Select(key => key.ToString()).ToArray()).ToArray();

            // assert
            Assert.That(allKeys, Is.Not.Empty);
        }

        [Test]
        public async Task Set_GivenEmptyStoreAndExpiryIs1Second_WhenFetchingAfter1Second_ThenItemsSetCannotBeRetrieved()
        {
            // arrange
            var load = TestType1Entities.Take(5).ToArray();

            // act
            await _cacheType1WithExpiry.Set(load);

            // assert
            var immediateItems = (await _cacheType1WithExpiry.GetAll()).ToArray();

            await Task.Delay(1500);

            var delayedItems = (await _cacheType1WithExpiry.GetAll()).ToArray();

            Assert.That(immediateItems.Select(v => v.Name), Is.EquivalentTo(load.Select(v => v.Name)));
            Assert.That(delayedItems, Is.Empty);
        }

        [Test]
        public async Task GetAll_WhenAnEntityOfDifferentTypeAreAdded_ThenAllCanBeRetrieved()
        {
            // arrange
            var firstEntityType1 = new TestType1Entity { Name = "test1_1", Id = "1" };
            var secondEntityType1 = new TestType1Entity { Name = "test1_2", Id = "2" };
            var firstEntityType2 = new TestType2Entity { Name = "test2_1", Id = "1" };
            var secondEntityType2 = new TestType2Entity { Name = "test2_2", Id = "2" };


            await _cacheType1.AddOrUpdate(firstEntityType1);
            await _cacheType1.AddOrUpdate(secondEntityType1);
                
            await _cacheType2.AddOrUpdate(firstEntityType2);
            await _cacheType2.AddOrUpdate(secondEntityType2);


            // act
            var retrievedAllType1Entities = (await _cacheType1.GetAll()).ToArray();
            var retrievedAllType2Entities = (await _cacheType2.GetAll()).ToArray();

            // assert
            Assert.That(retrievedAllType1Entities, Has.Length.EqualTo(2));
            Assert.That(retrievedAllType2Entities, Has.Length.EqualTo(2));
            Assert.That(retrievedAllType1Entities.Select(item => item.Name), Contains.Item(firstEntityType1.Name).And.Contains(secondEntityType1.Name));
            Assert.That(retrievedAllType2Entities.Select(item => item.Name), Contains.Item(firstEntityType2.Name).And.Contains(secondEntityType2.Name));
        }

        [Test]
        public async Task AddOrUpdate_WhenAnEntityIsAdded_ThenItCanBeRetrieved()
        {
            // arrange
            var entity = new TestType1Entity { Name = "test", Id = "1" };

            // act
            await _cacheType1.AddOrUpdate(entity);
            var retrievedEntity = await _cacheType1.Get(entity.Id);

            // assert
            Assert.That(retrievedEntity.Name, Is.EqualTo(entity.Name));
        }

        [Test]
        public async Task AddOrUpdate_WhenAnEntityIsAddedWithExpiryOf1Second_ThenItCannotBeRetrieved()
        {
            // arrange
            var entity = new TestType1Entity { Name = "test", Id = "1" };

            // act
            await _cacheType1WithExpiry.AddOrUpdate(entity);
            var retrievedEntityBeforeExpiry = await _cacheType1WithExpiry.Get(entity.Id);
            await Task.Delay(1500);
            var retrievedEntityAfterExpiry = await _cacheType1WithExpiry.Get(entity.Id);

            // assert
            Assert.That(retrievedEntityBeforeExpiry.Name, Is.EqualTo(entity.Name));
            Assert.That(retrievedEntityAfterExpiry, Is.Null);
        }

        [Test]
        public async Task AddOrUpdate_WhenTheSameKeyIsAddedTwice_ThenItIsAddedThenUpdated()
        {
            // arrange
            var firstEntity = new TestType1Entity { Name = "test_original", Id = "1" };
            var secondEntity = new TestType1Entity { Name = "test_changed", Id = "1" };

            // act
            await _cacheType1.AddOrUpdate(firstEntity);
            await _cacheType1.AddOrUpdate(secondEntity);

            var retrievedAllEntities = (await _cacheType1.GetAll()).ToArray();
            var retrievedEntity = (await _cacheType1.Get(firstEntity.Id));

            // assert
            Assert.That(retrievedEntity.Name, Is.EqualTo(secondEntity.Name)); // name updated properly
            Assert.That(retrievedAllEntities, Has.Length.EqualTo(1)); // no duplicate
        }

        [Test]
        public async Task AddOrUpdate_WhenEntityAlreadyExists_ThenEntityIsUpdated()
        {
            // arrange
            var entity = new TestType1Entity { Name = "test1", Id = "1" };
            await _cacheType1.Set(new[] { entity });

            // act
            entity.Name = "test1_changed";

            await _cacheType1.AddOrUpdate(entity);
            var retrievedEntity = await _cacheType1.Get(entity.Id);

            // assert
            Assert.That(retrievedEntity.Name, Is.EqualTo(entity.Name));
            Assert.That(retrievedEntity.Id, Is.EqualTo(entity.Id));
        }

        

        [Test]
        public async Task Remove_WhenAnEntityIsDeleted_ThenItCannotBeRetrieved()
        {
            // arrange
            var firstEntityType1 = new TestType1Entity { Name = "test1_1", Id = "1" };
            var secondEntityType1 = new TestType1Entity { Name = "test1_2", Id = "2" };
            var entityType2 = new TestType2Entity { Name = "test2", Id = "1" };

            await _cacheType1.Set(new[] { firstEntityType1, secondEntityType1 });
            await _cacheType2.Set(new[] { entityType2 });

            // act
            await _cacheType1.Remove(firstEntityType1.Id);

            var deletedEntityType1Found = await _cacheType1.Get(firstEntityType1.Id);
            var secondEntityType1Found = await _cacheType1.Get(secondEntityType1.Id);
            var entityType2Found = await _cacheType2.Get(entityType2.Id);

            // assert
            Assert.That(deletedEntityType1Found, Is.Null);
            Assert.That(secondEntityType1Found.Name, Is.EqualTo(secondEntityType1.Name));
            Assert.That(entityType2Found.Name, Is.EqualTo(entityType2.Name));
        }

        [Test]
        public async Task Remove_GivenEmptyStore_WhenItemAddedThenRemoved_ThenTheKeyMustBeFoundButEmpty()
        {
            // arrange
            var entity = new TestType1Entity { Name = "test1_1", Id = "1" };

            // act 
            await _cacheType1.AddOrUpdate(entity);
            await _cacheType1.Remove(entity.Id);

            var allKeys = _connection.GetEndPoints().SelectMany(endPoint => _connection.GetServer(endPoint).Keys().Select(key => key.ToString()).ToArray()).ToArray();

            // assert
            Assert.That(allKeys, Is.Not.Empty);
        }

        [Test]
        public async Task Remove_WhenEmptyStore_ShouldGracefullyPass()
        {
            // act
            await _cacheType1.Remove("any");

            // assert
            Assert.Pass();
        }

        [Test]
        public async Task Remove_WhenKeyNotFound_ShouldGracefullyPass()
        {
            // arrange
            var entity1 = new TestType1Entity { Name = "test1", Id = "1" };
            await _cacheType1.Set(new[] { entity1 });

            // act
            await _cacheType1.Remove("any");

            // assert
            Assert.Pass();
        }

        [Test]
        public async Task Flush_WhenAnEntityOfType1AndType2AreAdded_ThenFlushed_ThenStoreMustBeEmpty()
        {
            // arrange
            var entity1 = new TestType1Entity { Name = "test1", Id = "1" };
            var entity2 = new TestType2Entity { Name = "test2", Id = "1" };

            await _cacheType1.Set(new[] { entity1 });
            await _cacheType2.Set(new[] { entity2 });

            // act (flush type 1)
            await _cacheType1.Flush();
            var entity1Found = await _cacheType1.Get(entity1.Id);
            var entity2Found = await _cacheType2.Get(entity2.Id);

            // assert
            Assert.That(entity1Found, Is.Null);
            Assert.That(entity2Found.Name, Is.EqualTo(entity2.Name));

            // act (flush type 2)
            await _cacheType2.Flush();
            entity1Found = await _cacheType1.Get(entity1.Id);
            entity2Found = await _cacheType2.Get(entity2.Id);

            // assert
            Assert.That(entity1Found, Is.Null);
            Assert.That(entity2Found, Is.Null);
        }

       

        private static KeyValuePair<string, TestType1Entity> ToKeyValuePair(TestType1Entity entity) => new KeyValuePair<string, TestType1Entity>(entity.Id, entity);
        private static KeyValuePair<string, TestType2Entity> ToKeyValuePair(TestType2Entity entity) => new KeyValuePair<string, TestType2Entity>(entity.Id, entity);
    }
}