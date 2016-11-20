using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.IntegrationTests.Infrastructure;
using PEL.Framework.Redis.Serialization;
using PEL.Framework.Redis.Store;
using StackExchange.Redis;


namespace RedisCache.Tests
{
    

    [Explicit]
    [TestFixture]
    internal class RedisIndexedStoreTests 
    {
        //private RedisIndexedStore<TestType1Entity> _cacheType1;
        //private RedisIndexedStore<TestType2Entity> _cacheType2;
        //private RedisIndexedStore<TestType1Entity> _cacheType1WithExpiry;

        private ConnectionMultiplexer _multiplexer;
        private const string RedisConnectionOptions = "localhost:6379,allowAdmin=true"; //TODO(thierryr): move to config file (if we are using a specific config for CI env. or Integration Tests)

        private RedisIndexedStore<TestCompany> _cacheType1;
        private RedisIndexedStore<TestPerson> _cacheType2;
        private RedisIndexedStore<TestCompany> _cacheType1WithExpiry;

        private readonly RedisTestServer _server = new RedisTestServer(@"C:\Program Files\Redis");


        [OneTimeSetUp]
        public  async Task OneTimeSetUp()
        {
            _multiplexer = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));

            await _server.Start();
            _multiplexer = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));
            _connector = new RedisTestDatabaseConnector(_multiplexer);

            _cacheType1 = new RedisIndexedStore<TestCompany>(
                    new RedisTestDatabaseConnector(_multiplexer),
                    new DefaultJsonSerializer(),
                    new CollectionSettings<TestCompany>() { MasterKeyExtractor = new TestCompanyKeyExtractor() },
                    new []
                    {
                        new IndexSettings<TestCompany>() {Extractor  = new TestCompanyCategoryExtractor(), Unique = false, WithPayload = false},
                        new IndexSettings<TestCompany>() {Extractor  = new TestCompanyNameExtractor(), Unique = true, WithPayload = true },
                    }
                );


            _cacheType2 = new RedisIndexedStore<TestPerson>(
                    new RedisTestDatabaseConnector(_multiplexer),
                    new DefaultJsonSerializer(),
                    new CollectionSettings<TestPerson>() { MasterKeyExtractor = new TestPersonKeyExtractor() },
                    new[]
                    {
                        new IndexSettings<TestPerson>() {Extractor  = new TestPersonNameExtractor(), Unique = true, WithPayload = true },
                        new IndexSettings<TestPerson>() {Extractor  = new TestPersonNameExtractor(), Unique = true, WithPayload = false }
                    }
                );
        }

        private static readonly List<TestCompany> TestType1Entities = Enumerable.Range(0, 10).Select(i => new TestCompany { Name = $"testType1#{i}", Id = $"id#{i}" }).ToList();
        private RedisTestDatabaseConnector _connector;

        [SetUp]
        public async Task SetUp()
        {
            await _cacheType1.ClearAsync();
            await _cacheType2.ClearAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            //await _cacheType1.ClearAsync();
            //await _cacheType2.ClearAsync();
            //_multiplexer?.Dispose();
        }

        [Test]
        public async Task AddOrUpdate_WhenAnEntityIsAdded_ThenIdCanBeRetrieved()
        {
            // arrange
            var entity = new TestCompany { Name = "test", Id = "1", Category = "Cat"};

            // act
            await _cacheType1.AddOrUpdateAsync(entity);
            var retrievedEntityId = (await _cacheType1.GetMasterKeysByIndexAsync<TestCompanyNameExtractor>(entity.Name)).FirstOrDefault();

            // assert
            Assert.That(retrievedEntityId, Is.EqualTo(entity.Id));
        }

        [Test]
        public async Task AddOrUpdate_WhenAnEntityIsAdded_ThenItCanBeRetrievedByValue()
        {
            // arrange
            var entity = new TestCompany { Name = "test", Id = "1", Category = "Cat" };

            // act
            await _cacheType1.AddOrUpdateAsync(entity);
            var retrievedEntity = (await _cacheType1.GetItemsByIndexAsync<TestCompanyNameExtractor>(entity.Name)).FirstOrDefault();

            // assert
            Assert.That(retrievedEntity, Is.EqualTo(entity));
        }


        /*
        [Test]
        public async Task AddOrUpdate_WhenAnEntityIsAddedWithExpiryOf1Second_ThenItCannotBeRetrieved()
        {
            // arrange
            var entity = new TestCompany { Name = "test", Id = "1" };

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
                await _cacheType1.AddOrUpdate(new TestCompany { Name = $"name#{i}", Id = $"{i}" });
            }

            // act
            var getAllItems = (await _cacheType1.GetAll()).ToArray();

            List<TestCompany> manyGetItems = new List<TestCompany>();
            for (int i = 0; i < 20000; i++)
            {
                manyGetItems.Add(await _cacheType1.Get(i.ToString()));
            }

            List<TestCompany> indexGetItems = new List<TestCompany>();
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
            var entity = new TestCompany { Name = "test", Id = "1" };

            // act
            await _cacheType1.AddOrUpdate(entity);
            var retrievedEntity = (await _cacheType1.GetItemsByIndex("name", entity.Name)).FirstOrDefault();
            var retrievedEntityId = (await _cacheType1.GetKeysByIndex("name", entity.Name)).FirstOrDefault();

            var entityChanged = new TestCompany { Name = "changed", Id = "1" };
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
        */

    }
}