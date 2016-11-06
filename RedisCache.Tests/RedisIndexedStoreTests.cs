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


        static TestType1Entity apple = new TestType1Entity { Name = "apple", Id = "A", Category = "tech" };
        static TestType1Entity boeing = new TestType1Entity { Name = "boeing", Id = "B", Category = "aero" };
        static TestType1Entity cargill = new TestType1Entity { Name = "cargill", Id = "C", Category = "food" };
        static TestType1Entity dell = new TestType1Entity { Name = "dell", Id = "D", Category = "tech" };
        static TestType1Entity ebay = new TestType1Entity { Name = "ebay", Id = "E", Category = "tech" };

        static TestType1Entity[] allCompanies = { apple, boeing, cargill, dell, ebay };

        [OneTimeSetUp]
        public  void OneTimeSetUp()
        {
            _connection = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));
            _cacheType1 = new RedisIndexedStore<TestType1Entity>(_connection, 
                new JsonSerializer(), 
                entity => entity.Id, 
                new[] {
                    IndexDefinition<TestType1Entity>.CreateUniqueFromExtractor("name", e => e.Name),
                    IndexDefinition<TestType1Entity>.CreateNonUniqueFromExtractor("category", e => e.Category)
                }, null);

            _cacheType2 = new RedisIndexedStore<TestType2Entity>(_connection, new JsonSerializer(), entity => entity.Id, new[] { IndexDefinition<TestType2Entity>.CreateUniqueFromExtractor("name", e => e.Name) }, null);
            _cacheType1WithExpiry = new RedisIndexedStore<TestType1Entity>(_connection, new JsonSerializer(), entity => entity.Id, new[] { IndexDefinition<TestType1Entity>.CreateUniqueFromExtractor("name", e => e.Name) }, TimeSpan.FromSeconds(1));
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
            var retrievedEntity = (await _cacheType1.GetItemsByIndex("name",entity.Name)).FirstOrDefault();
            var retrievedEntityId = (await _cacheType1.GetKeysByIndex("name",entity.Name)).FirstOrDefault();

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
            var retrievedEntityBeforeExpiry = (await _cacheType1.GetItemsByIndex("name", entity.Name)).FirstOrDefault();

            await Task.Delay(1500);
            var retrievedEntityAfterExpiry = (await _cacheType1.GetItemsByIndex("name", entity.Name)).FirstOrDefault();
            var retrievedEntityIdAfterExpiry = (await _cacheType1.GetKeysByIndex("name", entity.Name)).FirstOrDefault();

            // assert
            Assert.That(retrievedEntityBeforeExpiry.Name, Is.EqualTo(entity.Name));
            Assert.That(retrievedEntityBeforeExpiry.Id, Is.EqualTo(entity.Id));

            Assert.That(retrievedEntityAfterExpiry, Is.Null);
            Assert.That(retrievedEntityIdAfterExpiry, Is.Null);

        }



        [Test]
        public async Task GetAll_WhenStoreIsFull_ThenRemoveAll_ShouldBeEmpty()
        {
            // arrange
            for (int i = 0; i < 20000; i++)
            {
                await _cacheType1.AddOrUpdate(new TestType1Entity { Name = $"name#{i}", Id = $"{i}" });
            }

            // act
            var getAllItems = (await _cacheType1.GetAll()).ToArray();

            List<TestType1Entity> manyGetItems = new List<TestType1Entity>();
            for (int i = 0; i < 20000; i++)
            {
                manyGetItems.Add(await _cacheType1.Get(i.ToString()));
            }

            List<TestType1Entity> indexGetItems = new List<TestType1Entity>();
            for (int i = 0; i < 20000; i++)
            {
                indexGetItems.Add((await _cacheType1.GetItemsByIndex("name", $"name#{i}")).First());
            }

            // assert
            Assert.That(manyGetItems, Is.EquivalentTo(getAllItems));
            Assert.That(indexGetItems, Is.EquivalentTo(getAllItems));
        }

        [Test]
        public async Task AddOrUpdate_WhenAnEntityIsAdded_ThenUniqueIndexMustBeChanged()
        {
            // arrange
            var entity = new TestType1Entity { Name = "test", Id = "1" };

            // act
            await _cacheType1.AddOrUpdate(entity);
            var retrievedEntity = (await _cacheType1.GetItemsByIndex("name", entity.Name)).FirstOrDefault();
            var retrievedEntityId = (await _cacheType1.GetKeysByIndex("name", entity.Name)).FirstOrDefault();

            var entityChanged = new TestType1Entity { Name = "changed", Id = "1" };
            await _cacheType1.AddOrUpdate(entityChanged);

            retrievedEntity = (await _cacheType1.GetItemsByIndex("name", entity.Name)).FirstOrDefault();
            retrievedEntityId = (await _cacheType1.GetKeysByIndex("name", entity.Name)).FirstOrDefault();

            var retrievedChangedEntity = (await _cacheType1.GetItemsByIndex("name", entityChanged.Name)).FirstOrDefault();
            var retrievedChangedEntityId = (await _cacheType1.GetKeysByIndex("name", entityChanged.Name)).FirstOrDefault();

            // assert
            Assert.That(retrievedEntityId, Is.Null);
            Assert.That(retrievedEntity, Is.Null);
            Assert.That(retrievedChangedEntityId, Is.EqualTo(entityChanged.Id));
            Assert.That(retrievedChangedEntity.Name, Is.EqualTo(entityChanged.Name));
        }

        [Test]
        public async Task Scenario1()
        {
            // arrange
            await _cacheType1.Set(allCompanies);

            var retrievedAppleByName = (await _cacheType1.GetItemsByIndex("name", nameof(apple))).FirstOrDefault();
            var retrievedCompaniesByGroup = (await _cacheType1.GetItemsByIndex("category", "tech"));

            Assert.That(retrievedAppleByName, Is.EqualTo(apple));
            Assert.That(retrievedCompaniesByGroup, Has.Member(apple));

            await _cacheType1.Remove(apple.Id);

            retrievedAppleByName = (await _cacheType1.GetItemsByIndex("name", nameof(apple))).FirstOrDefault();
            retrievedCompaniesByGroup = (await _cacheType1.GetItemsByIndex("category", "tech"));

            Assert.That(retrievedAppleByName, Is.Null);
            Assert.That(retrievedCompaniesByGroup, Has.No.Member(apple));

            foreach (var item in retrievedCompaniesByGroup)
            {
                await _cacheType1.Remove(item.Id);
            }

            retrievedCompaniesByGroup = (await _cacheType1.GetItemsByIndex("category", "tech"));
            Assert.That(retrievedCompaniesByGroup, Is.Empty);

        }


    }
}