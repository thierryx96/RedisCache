using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Database;
using PEL.Framework.Redis.IntegrationTests.Infrastructure;
using PEL.Framework.Redis.Serialization;
using PEL.Framework.Redis.Store;
using StackExchange.Redis;

namespace PEL.Framework.Redis.IntegrationTests.Server
{
    /// <summary>
    ///     Utils methods on a Sever and Database level
    /// </summary>
    [TestFixture]
    internal class RedisDatabaseManagerTests
    {
        [SetUp]
        public async Task SetUp()
        {
            await _databaseManager.FlushAll();
            await Task.WhenAll(_cache1.ClearAsync(), _cache2.ClearAsync());
        }

        private readonly RedisTestServer _server = new RedisTestServer(@"C:\Program Files\Redis");
        private RedisDatabaseManager _databaseManager;
        private RedisTestDatabaseConnector _connection;
        private ConnectionMultiplexer _multiplexer;
        private RedisStore<TestCompany> _cache1;
        private RedisStore<TestPerson> _cache2;
        private const string RedisConnectionOptions = "localhost:6379,allowAdmin=true";

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await _server.Start();

            _multiplexer = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(RedisConnectionOptions));
            _connection = new RedisTestDatabaseConnector(_multiplexer);
            _cache1 = new RedisStore<TestCompany>(
                new RedisTestDatabaseConnector(_multiplexer),
                new DefaultJsonSerializer(),
                new CollectionSettings<TestCompany> {MasterKeyExtractor = new TestCompanyKeyExtractor()}
            );

            _cache2 = new RedisStore<TestPerson>(
                new RedisTestDatabaseConnector(_multiplexer),
                new DefaultJsonSerializer(),
                new CollectionSettings<TestPerson> {MasterKeyExtractor = new TestPersonKeyExtractor()});
            _databaseManager = new RedisDatabaseManager(_connection);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _databaseManager.FlushAll();

            _multiplexer?.Dispose();

            _server?.Dispose();
        }


        [Test]
        public async Task FlushAll_WhenStoreGotSomeKeys_ShouldLeaveStoreEmpty()
        {
            // arrange
            await Task.WhenAll(
                _cache1.AddOrUpdateAsync(new TestCompany {Id = "a"}),
                _cache2.AddOrUpdateAsync(new TestPerson {Id = "b"})
            );

            // act
            await _databaseManager.FlushAll();

            // assert
            var allkeys = _databaseManager.ScanKeys().ToList();

            Assert.That(allkeys, Is.Empty);
        }

        [Test]
        public async Task ScanKeys_ShouldRetrieveAllKeys()
        {
            // arrange
            await Task.WhenAll(
                _cache1.AddOrUpdateAsync(new TestCompany {Id = "a"}),
                _cache2.AddOrUpdateAsync(new TestPerson {Id = "b"}));

            // act
            var allkeys = _databaseManager.ScanKeys().ToArray();

            // assert
            Assert.That(allkeys, Has.Length.EqualTo(2));
        }

        [Test]
        public async Task ScanKeys_WhenUsingPatternMatchingOneKey_ShouldRetrieveThatKeys()
        {
            // arrange
            await Task.WhenAll(
                _cache1.AddOrUpdateAsync(new TestCompany {Id = "a"}), // key of hashset : testcompany
                _cache2.AddOrUpdateAsync(new TestPerson {Id = "b"}) // key of hashset : testperson
            );

            // act
            var allkeys = _databaseManager.ScanKeys($"{nameof(TestCompany).ToLowerInvariant()}*").ToArray();

            // assert
            Assert.That(allkeys, Has.Length.EqualTo(1));
            Assert.That(allkeys.Single().Contains(nameof(TestCompany).ToLowerInvariant()));
        }
    }
}