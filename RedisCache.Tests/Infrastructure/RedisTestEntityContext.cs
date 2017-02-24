using System;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Serialization;
using PEL.Framework.Redis.Store;
using StackExchange.Redis;

namespace PEL.Framework.Redis.IntegrationTests.Infrastructure
{
    internal class CacheCreator
    {
        public RedisStore<TestCompany> CreateTestCompanyCache(ConnectionMultiplexer multiplexer, TimeSpan? expiry)
        {
            return new RedisStore<TestCompany>(
                new RedisTestDatabaseConnector(multiplexer),
                new DefaultJsonSerializer(),
                new CollectionSettings<TestCompany>
                {
                    Expiry = expiry,
                    MasterKeyExtractor = new TestCompanyKeyExtractor()
                }
            );
        }
    }

    internal class TestCompanyKeyExtractor : IKeyExtractor<TestCompany>
    {
        public string ExtractKey(TestCompany value) => value.Id;
    }

    internal class TestPersonKeyExtractor : IKeyExtractor<TestPerson>
    {
        public string ExtractKey(TestPerson value) => value.Id;
    }

    internal class TestCompanyCategoryExtractor : IKeyExtractor<TestCompany>
    {
        public string ExtractKey(TestCompany value) => value.Category;
    }

    internal class TestCompanyNameExtractor : IKeyExtractor<TestCompany>
    {
        public string ExtractKey(TestCompany value) => value.Name;
    }

    internal class TestPersonNameExtractor : IKeyExtractor<TestPerson>
    {
        public string ExtractKey(TestPerson value) => $"{value.FirstName}-{value.LastName}";
    }

    internal class TestPersonEmployerSectorExtractor : IKeyExtractor<TestPerson>
    {
        public string ExtractKey(TestPerson value) => value.Employer?.Category;
    }


    internal class TestCompany : IEquatable<TestCompany>
    {
        internal static readonly TestCompany Apple = new TestCompany
        {
            Name = "apple",
            Id = "A",
            Category = "tech"
        };

        internal static readonly TestCompany Boeing = new TestCompany
        {
            Name = "boeing",
            Id = "B",
            Category = "aero"
        };

        internal static readonly TestCompany Cargill = new TestCompany
        {
            Name = "cargill",
            Id = "C",
            Category = "food"
        };

        internal static readonly TestCompany Dell = new TestCompany
        {
            Name = "dell",
            Id = "D",
            Category = "tech"
        };

        internal static readonly TestCompany Ebay = new TestCompany
        {
            Name = "ebay",
            Id = "E",
            Category = "tech"
        };

        internal static readonly TestCompany[] AllCompanies = {Apple, Boeing, Cargill, Dell, Ebay};

        public TestCompany()
        {
        }

        public TestCompany(string id, string name, string category)
        {
            Category = category;
            Id = id;
            Name = name;
            NestedProperty = new ItemType();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public ItemType NestedProperty { get; set; }

        public bool Equals(TestCompany other)
        {
            return other != null && Id == other.Id && Name == other.Name && Category == other.Category;
        }

        internal class ItemType
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }

    internal class TestPerson : IEquatable<TestPerson>
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public TestCompany Employer { get; set; }

        public bool Equals(TestPerson other)
        {
            return other != null
                   && string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(FirstName, other.FirstName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(LastName, other.LastName, StringComparison.OrdinalIgnoreCase)
                   && Equals(Employer, other.Employer);
        }
    }
}