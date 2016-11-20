
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using PEL.Framework.Redis.Configuration;
    using PEL.Framework.Redis.Database;
    using PEL.Framework.Redis.IntegrationTests.Infrastructure;
    using PEL.Framework.Redis.Serialization;
    using PEL.Framework.Redis.Store;
    using StackExchange.Redis;

    namespace PEL.Framework.Redis.IntegrationTests.Store
    {
        [TestFixture]
        internal class RedisStoreTests
        {
            protected RedisStore<TestCompany> _cacheType1;
            protected RedisStore<TestPerson> _cacheType2;
            protected RedisStore<TestCompany> _cacheType1WithExpiry;
            private readonly RedisTestServer _server = new RedisTestServer(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Redis");

            private RedisTestDatabaseConnector _connection;
            private const string RedisConnectionOptions = "localhost:6379,allowAdmin=true";
            private RedisDatabaseManager _database;

            [OneTimeSetUp]
            public async Task OneTimeSetUp()
            {
                //_server.KillAll();
                await _server.Start();
                _multiplexer = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));
                _connection = new RedisTestDatabaseConnector(_multiplexer);

                _cacheType1 = new RedisStore<TestCompany>(
                        new RedisTestDatabaseConnector(_multiplexer),
                        new DefaultJsonSerializer(),
                        new CollectionSettings<TestCompany>() {  MasterKeyExtractor = new TestCompanyKeyExtractor() }
                    );

                _cacheType2 = new RedisStore<TestPerson>(
                    new RedisTestDatabaseConnector(_multiplexer),
                    new DefaultJsonSerializer(),
                    new CollectionSettings<TestPerson>() {MasterKeyExtractor = new TestPersonKeyExtractor()});

                _cacheType1WithExpiry = new RedisStore<TestCompany>(
                        new RedisTestDatabaseConnector(_multiplexer),
                        new DefaultJsonSerializer(),
                        new CollectionSettings<TestCompany>() { MasterKeyExtractor = new TestCompanyKeyExtractor(), Expiry = TimeSpan.FromSeconds(1) });

                _database = new RedisDatabaseManager(_connection);
            }

            private static readonly List<TestCompany> TestType1Entities = Enumerable.Range(0, 10).Select(i => new TestCompany { Name = $"testType1#{i}", Id = $"id#{i}" }).ToList();
            private ConnectionMultiplexer _multiplexer;

            [SetUp]
            public async Task SetUp()
            {
                await _database.FlushAll();
                //await Task.WhenAll(_cacheType1.ClearAsync(), _cacheType2.ClearAsync());
            }

            [OneTimeTearDown]
            public async Task OneTimeTearDown()
            {
                await Task.WhenAll(_cacheType1.ClearAsync(), _cacheType2.ClearAsync());

                _multiplexer?.Dispose();

                _server?.Dispose();
            }

            [Test]
            public async Task Set_WhenManyEntitiesAreLoadedThenOneAdded_ThenAllCanBeRetrieved()
            {
                // arrange
                var firstLoad = TestType1Entities.Take(5).ToArray();
                var lastLoad = TestType1Entities.Skip(5).ToArray();
                var expected = firstLoad.Union(lastLoad).ToArray();

                // act
                await _cacheType1.SetAsync(firstLoad);

                // assert
                var retrievedFirstLoad = (await _cacheType1.GetAllAsync()).ToArray();
                Assert.That(retrievedFirstLoad, Is.EquivalentTo(firstLoad));

                // act
                foreach (var entity in lastLoad)
                {
                    await _cacheType1.AddOrUpdateAsync(entity);
                }

                var retrievedAllEntities = (await _cacheType1.GetAllAsync()).ToArray();

                // assert
                Assert.That(retrievedAllEntities, Is.EquivalentTo(expected));
            }

            [Test]
            public async Task Set_GivenEmptyStore_WhenItemsAddedAreEmpty_ThenTheKeyMustBeFoundAndContainsNoItems()
            {
                // arrange
                var emptyCollection = new TestCompany[] { };

                // act
                await _cacheType1.SetAsync(emptyCollection);
                var allKeys = _database.ScanKeys();

                // assert
                Assert.That(allKeys, Is.Empty);
            }

            [Test]
            public async Task Set_GivenEmptyStoreAndExpiryIs1Second_WhenFetchingAfter1Second_ThenItemsSetCannotBeRetrieved()
            {
                // arrange
                var load = TestType1Entities.Take(5).ToArray();

                // act
                await _cacheType1WithExpiry.SetAsync(load);

                // assert
                var immediateItems = (await _cacheType1WithExpiry.GetAllAsync()).ToArray();

                await Task.Delay(1500);

                var delayedItems = (await _cacheType1WithExpiry.GetAllAsync()).ToArray();

                Assert.That(immediateItems, Is.EquivalentTo(load));
                Assert.That(delayedItems, Is.Empty);
            }

            [Test]
            public async Task GetAll_WhenAnEntityOfDifferentTypeAreAdded_ThenAllCanBeRetrieved()
            {
                // arrange
                var type1Entities = new[] { new TestCompany { Name = "test1_1", Id = "1" }, new TestCompany { Name = "test1_2", Id = "2" } };
                var type2Entities = new[] { new TestPerson { FirstName = "test2_1", Id = "1" }, new TestPerson { FirstName = "test2_2", Id = "2" } };

                await _cacheType1.AddOrUpdateAsync(type1Entities.First());
                await _cacheType1.AddOrUpdateAsync(type1Entities.Last());
                await _cacheType2.AddOrUpdateAsync(type2Entities.First());
                await _cacheType2.AddOrUpdateAsync(type2Entities.Last());

                // act
                var retrievedAllType1Entities = (await _cacheType1.GetAllAsync()).ToArray();
                var retrievedAllType2Entities = (await _cacheType2.GetAllAsync()).ToArray();

                // assert
                Assert.That(retrievedAllType1Entities, Has.Length.EqualTo(2));
                Assert.That(retrievedAllType2Entities, Has.Length.EqualTo(2));
                Assert.That(retrievedAllType1Entities, Is.EquivalentTo(type1Entities));
                Assert.That(retrievedAllType2Entities, Is.EquivalentTo(type2Entities));
            }

            [Test]
            public async Task AddOrUpdate_WhenAnEntityIsAdded_ThenItCanBeRetrieved()
            {
                // arrange
                var entity = new TestCompany { Name = "test", Id = "1" };

                // act
                await _cacheType1.AddOrUpdateAsync(entity);
                var retrievedEntity = await _cacheType1.GetAsync(entity.Id);

                // assert
                Assert.That(retrievedEntity, Is.EqualTo(entity));
            }

            [Test]
            public async Task AddOrUpdate_WhenAnEntityIsAddedWithExpiryOf1Second_ThenItCannotBeRetrieved()
            {
                // arrange
                var entity = new TestCompany { Name = "test", Id = "1" };

                // act
                await _cacheType1WithExpiry.AddOrUpdateAsync(entity);
                var retrievedEntityBeforeExpiry = await _cacheType1WithExpiry.GetAsync(entity.Id);
                await Task.Delay(1500);
                var retrievedEntityAfterExpiry = await _cacheType1WithExpiry.GetAsync(entity.Id);

                // assert
                Assert.That(retrievedEntityBeforeExpiry, Is.EqualTo(entity));
                Assert.That(retrievedEntityAfterExpiry, Is.Null);
            }

            [Test]
            public async Task AddOrUpdate_WhenTheSameKeyIsAddedTwice_ThenItIsAddedThenUpdated()
            {
                // arrange
                var firstEntity = new TestCompany { Name = "test_original", Id = "1" };
                var secondEntity = new TestCompany { Name = "test_changed", Id = "1" };

                // act
                await _cacheType1.AddOrUpdateAsync(firstEntity);
                await _cacheType1.AddOrUpdateAsync(secondEntity);

                var retrievedAllEntities = (await _cacheType1.GetAllAsync()).ToArray();
                var retrievedEntity = (await _cacheType1.GetAsync(firstEntity.Id));

                // assert
                Assert.That(retrievedEntity, Is.EqualTo(secondEntity)); // name updated properly
                Assert.That(retrievedAllEntities, Has.Length.EqualTo(1)); // no duplicate
            }

            [Test]
            public async Task AddOrUpdate_WhenEntityAlreadyExists_ThenEntityIsUpdated()
            {
                // arrange
                var entity = new TestCompany { Name = "test1", Id = "1" };
                await _cacheType1.SetAsync(new[] { entity });

                // act
                entity.Name = "test1_changed";

                await _cacheType1.AddOrUpdateAsync(entity);
                var retrievedEntity = await _cacheType1.GetAsync(entity.Id);

                // assert
                Assert.That(retrievedEntity, Is.EqualTo(entity));
            }

            [Test]
            public async Task Remove_WhenAnEntityIsDeleted_ThenItCannotBeRetrieved()
            {
                // arrange
                var firstEntityType1 = new TestCompany { Name = "test1_1", Id = "1" };
                var secondEntityType1 = new TestCompany { Name = "test1_2", Id = "2" };
                var entityType2 = new TestPerson { FirstName = "test2", Id = "1" };

                await _cacheType1.SetAsync(new[] { firstEntityType1, secondEntityType1 });
                await _cacheType2.SetAsync(new[] { entityType2 });

                // act
                await _cacheType1.RemoveAsync(firstEntityType1.Id);

                var deletedEntityType1Found = await _cacheType1.GetAsync(firstEntityType1.Id);
                var secondEntityType1Found = await _cacheType1.GetAsync(secondEntityType1.Id);
                var entityType2Found = await _cacheType2.GetAsync(entityType2.Id);

                // assert
                Assert.That(deletedEntityType1Found, Is.Null);
                Assert.That(secondEntityType1Found, Is.EqualTo(secondEntityType1));
                Assert.That(entityType2Found, Is.EqualTo(entityType2));
            }

            [Test]
            public async Task Remove_GivenEmptyStore_WhenItemAddedThenRemoved_ThenTheKeyMustBeFoundButEmpty()
            {
                // arrange
                var entity = new TestCompany { Name = "test1_1", Id = "1" };

                // act 
                await _cacheType1.AddOrUpdateAsync(entity);
                await _cacheType1.RemoveAsync(entity.Id);

                var allKeys = _database.ScanKeys();

                // assert
                Assert.That(allKeys, Is.Empty);
            }

            [Test]
            public void Remove_WhenEmptyStore_ShouldGracefullyPass()
            {
                // act + assert
                Assert.That(async () => await _cacheType1.RemoveAsync("any"), Throws.Nothing);
            }

            [Test]
            public void Remove_WhenKeyNotFound_ShouldGracefullyPass()
            {
                // arrange
                var entity1 = new TestCompany { Name = "test1", Id = "1" };

                // act + assert
                Assert.That(async () => await _cacheType1.RemoveAsync(entity1.Id), Throws.Nothing);
                Assert.That(async () => await _cacheType1.RemoveAsync("any"), Throws.Nothing);
                Assert.That(async () => await _cacheType1.RemoveAsync("any"), Throws.Nothing);
            }

            [Test]
            public async Task Flush_WhenAnEntityOfType1AndType2AreAdded_ThenFlushed_ThenStoreMustBeEmpty()
            {
                // arrange
                var entity1 = new TestCompany { Name = "test1", Id = "1" };
                var entity2 = new TestPerson { FirstName = "test2", Id = "1" };

                await _cacheType1.SetAsync(new[] { entity1 });
                await _cacheType2.SetAsync(new[] { entity2 });

                // act (flush type 1)
                await _cacheType1.ClearAsync();
                var entity1Found = await _cacheType1.GetAsync(entity1.Id);
                var entity2Found = await _cacheType2.GetAsync(entity2.Id);

                // assert
                Assert.That(entity1Found, Is.Null);
                Assert.That(entity2Found, Is.EqualTo(entity2));

                // act (flush type 2)
                await _cacheType2.ClearAsync();
                entity1Found = await _cacheType1.GetAsync(entity1.Id);
                entity2Found = await _cacheType2.GetAsync(entity2.Id);

                // assert
                Assert.That(entity1Found, Is.Null);
                Assert.That(entity2Found, Is.Null);
            }
        }
    }

