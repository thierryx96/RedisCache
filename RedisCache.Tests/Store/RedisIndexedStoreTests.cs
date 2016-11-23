using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.IntegrationTests.Infrastructure;
using PEL.Framework.Redis.Serialization;
using PEL.Framework.Redis.Store;
using StackExchange.Redis;

namespace PEL.Framework.Redis.IntegrationTests.Store
{
    [TestFixture]
    internal class RedisIndexedStoreTests
    {
        private ConnectionMultiplexer _multiplexer;
        private const string RedisConnectionOptions = "localhost:6379,allowAdmin=true"; //TODO(thierryr): move to config file (if we are using a specific config for CI env. or Integration Tests)

        private RedisIndexedStore<TestCompany> _cacheType1;
        private RedisIndexedStore<TestPerson> _cacheType2;
        private readonly RedisTestServer _server = new RedisTestServer(@"C:\Program Files\Redis");

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _multiplexer = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));

            await _server.Start();
            _multiplexer = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));

            _cacheType1 = new RedisIndexedStore<TestCompany>(
                new RedisTestDatabaseConnector(_multiplexer),
                new DefaultJsonSerializer(),
                new CollectionWithIndexesSettings<TestCompany>
                {
                    MasterKeyExtractor = new TestCompanyKeyExtractor(),
                    Indexes = new[]
                    {
                        new IndexSettings<TestCompany> { Extractor = new TestCompanyNameExtractor(), Unique = true, WithPayload = true },
                        new IndexSettings<TestCompany> { Extractor = new TestCompanyCategoryExtractor(), Unique = false, WithPayload = true }
                    }
                }
            );


            _cacheType2 = new RedisIndexedStore<TestPerson>(
                new RedisTestDatabaseConnector(_multiplexer),
                new DefaultJsonSerializer(),
                new CollectionWithIndexesSettings<TestPerson>
                {
                    MasterKeyExtractor = new TestPersonKeyExtractor(),
                    Indexes = new[]
                    {
                        new IndexSettings<TestPerson> { Extractor = new TestPersonNameExtractor(), Unique = true, WithPayload = true },
                        new IndexSettings<TestPerson> { Extractor = new TestPersonEmployerSectorExtractor(), Unique = false, WithPayload = true }
                    }
                }
            );
        }

        [SetUp]
        public async Task SetUp()
        {
            await _cacheType1.ClearAsync();
            await _cacheType2.ClearAsync();
        }

        [Test]
        public async Task RetrieveByUniqueIndex_WhenACompanyIsAdded_ThenItCanBeRetrieved()
        {
            // arrange
            var entity = new TestCompany { Name = "test", Id = "1", Category = "Cat" };

            // act
            await _cacheType1.AddOrUpdateAsync(entity);
            var retrievedEntityId = (await _cacheType1.GetMasterKeysByIndexAsync<TestCompanyNameExtractor>(entity.Name)).FirstOrDefault();
            var retrievedEntity = (await _cacheType1.GetItemsByIndexAsync<TestCompanyNameExtractor>(entity.Name)).FirstOrDefault();

            // assert
            Assert.That(retrievedEntityId, Is.EqualTo(entity.Id));
            Assert.That(retrievedEntity, Is.EqualTo(entity));
        }

        [Test]
        public async Task RetrieveByUniqueIndex_WhenCompaniesNamesAreUpdated_ThenIndexedNamesAreCorrect()
        {
            // arrange
            await _cacheType1.SetAsync(TestCompany.AllCompanies);

            var updatedBoeing = new TestCompany(TestCompany.Boeing.Id, "new Boeing", TestCompany.Boeing.Category);
            var updatedEbay = new TestCompany(TestCompany.Ebay.Id, "new Ebay", TestCompany.Ebay.Category);

            // re-add Boeing & Ebay updated to expected result
            var expectedCompanies = TestCompany.AllCompanies
                .Except(new[] { TestCompany.Boeing, TestCompany.Ebay })
                .Concat(new[] { updatedBoeing, updatedEbay });

            // update Boeing & Ebay in the actual cache
            await _cacheType1.AddOrUpdateAsync(updatedBoeing);
            await _cacheType1.AddOrUpdateAsync(updatedEbay);

            // companies can be retrieved
            foreach (var expectedCompany in expectedCompanies)
            {
                // act
                var retrievedEntity = (await _cacheType1.GetItemsByIndexAsync<TestCompanyNameExtractor>(expectedCompany.Name)).Single();
                var retrievedEntityId = (await _cacheType1.GetMasterKeysByIndexAsync<TestCompanyNameExtractor>(expectedCompany.Name)).Single();

                // assert
                Assert.That(retrievedEntity, Is.EqualTo(expectedCompany));
                Assert.That(retrievedEntityId, Is.EqualTo(expectedCompany.Id));
            }

            // old names don't lead to results
            foreach (var oldCompany in new[] { TestCompany.Boeing, TestCompany.Ebay })
            {
                // act
                var retrievedEntities = (await _cacheType1.GetItemsByIndexAsync<TestCompanyNameExtractor>(oldCompany.Name));
                var retrievedEntityIds = (await _cacheType1.GetMasterKeysByIndexAsync<TestCompanyNameExtractor>(oldCompany.Name));

                // assert
                Assert.That(retrievedEntities, Is.Empty);
                Assert.That(retrievedEntityIds, Is.Empty);
            }
        }

        [Test]
        public async Task RetrieveByLookupIndex_WhenASingleCompanyIsAdded_ThenItCanBeRetrieved()
        {
            // arrange
            var entity = new TestCompany { Name = "test", Id = "1", Category = "Cat" };

            // act
            await _cacheType1.AddOrUpdateAsync(entity);
            var retrievedEntity = (await _cacheType1.GetItemsByIndexAsync<TestCompanyCategoryExtractor>(entity.Category)).FirstOrDefault();
            var retrievedEntityId = (await _cacheType1.GetMasterKeysByIndexAsync<TestCompanyCategoryExtractor>(entity.Category)).FirstOrDefault();

            // assert
            Assert.That(retrievedEntity, Is.EqualTo(entity));
            Assert.That(retrievedEntityId, Is.EqualTo(entity.Id));
        }

        [Test]
        public async Task RetrieveByLookupIndex_WhenCompaniesAreAdded_ThenIndexGroupsAreCorrect()
        {
            // arrange
            await _cacheType1.SetAsync(TestCompany.AllCompanies);
            var groupedCompanies = TestCompany.AllCompanies.ToLookup(company => company.Category);


            foreach (var group in groupedCompanies)
            {
                // act
                var retrievedCategoryEntities = (await _cacheType1.GetItemsByIndexAsync<TestCompanyCategoryExtractor>(group.Key));
                var retrievedCategoryEntityIds = (await _cacheType1.GetMasterKeysByIndexAsync<TestCompanyCategoryExtractor>(group.Key));

                // assert
                Assert.That(retrievedCategoryEntities, Is.EquivalentTo(group.ToArray()));
                Assert.That(retrievedCategoryEntityIds, Is.EquivalentTo(group.Select(company => company.Id)));
            }
        }

        [Test]
        public async Task RetrieveByLookupIndex_WhenCompaniesAreRemoved_ThenIndexGroupsAreCorrect()
        {
            // arrange
            await _cacheType1.SetAsync(TestCompany.AllCompanies);

            var removedCompanies = new[] { TestCompany.Boeing, TestCompany.Cargill };
            var groupedCompanies = TestCompany.AllCompanies.Except(removedCompanies).ToLookup(company => company.Category);

            await _cacheType1.RemoveAsync(removedCompanies.Select(company => company.Id));

            foreach (var group in groupedCompanies)
            {
                // act
                var retrievedCategoryEntities = (await _cacheType1.GetItemsByIndexAsync<TestCompanyCategoryExtractor>(group.Key));
                var retrievedCategoryEntityIds = (await _cacheType1.GetMasterKeysByIndexAsync<TestCompanyCategoryExtractor>(group.Key));

                // assert
                Assert.That(retrievedCategoryEntities, Is.EquivalentTo(group.ToArray()));
                Assert.That(retrievedCategoryEntityIds, Is.EquivalentTo(group.Select(company => company.Id)));
            }
        }

        [Test]
        public async Task RetrieveByLookupIndex_WhenCompaniesAreUpdated_ThenIndexGroupsAreCorrect()
        {
            // arrange
            await _cacheType1.SetAsync(TestCompany.AllCompanies);

            var updatedBoeing = TestCompany.Boeing;
            updatedBoeing.Category = TestCompany.Apple.Category; // boeing is now tech

            var updatedEbay = TestCompany.Ebay;
            updatedBoeing.Category = "ecom"; // ebay is actually e-com

            // re-add Boeing & Ebay updated to expected result
            var groupedCompanies = TestCompany.AllCompanies
                .Except(new[] { TestCompany.Boeing, TestCompany.Ebay })
                .Concat(new[] { updatedBoeing, updatedEbay })
                .ToLookup(company => company.Category);

            // update Boeing & Ebay in the actual cache
            await _cacheType1.AddOrUpdateAsync(updatedBoeing);
            await _cacheType1.AddOrUpdateAsync(updatedEbay);

            foreach (var group in groupedCompanies)
            {
                // act
                var retrievedCategoryEntities = (await _cacheType1.GetItemsByIndexAsync<TestCompanyCategoryExtractor>(group.Key));
                var retrievedCategoryEntityIds = (await _cacheType1.GetMasterKeysByIndexAsync<TestCompanyCategoryExtractor>(group.Key));

                // assert
                Assert.That(retrievedCategoryEntities, Is.EquivalentTo(group.ToArray()));
                Assert.That(retrievedCategoryEntityIds, Is.EquivalentTo(group.Select(company => company.Id)));
            }
        }
    }
}