using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache
{


    public static class RedisExtensions
    {

        //public static string GenerateHashIndexName(string collectionRootName, string indexName) => $"{collectionRootName}:{indexName.ToLowerInvariant()}";
        //public static string GenerateHashReverseIndexName(string collectionRootName, string indexName) => $"{collectionRootName}:{indexName.ToLowerInvariant()}";

        //public static string GenerateSetIndexName(string collectionRootName, string indexName, string indexKey) => $"{collectionRootName}:{indexName.ToLowerInvariant()}:{indexKey.ToLowerInvariant()}";

        // Get values prepared in a redis hash
        public static HashEntry[] ToHashEntries<TValue>(this IEnumerable<TValue> items, Func<TValue, string> entryKeyExtractor, Func<TValue, string> valueKeyExtractor) => items.Select(item => new HashEntry(entryKeyExtractor(item), valueKeyExtractor(item))).ToArray();

        // Get values prepared in a redis set
        public static ILookup<string, RedisValue> ToMappings<TValue>(this IEnumerable<TValue> items, Func<TValue, string> mappingNameExtractor, Func<TValue, string> setItemExtrator) => items.Where(item => mappingNameExtractor(item) != null).ToLookup(item => mappingNameExtractor(item), item => (RedisValue)setItemExtrator(item));

        /*
        public static void RemoveFromUniqueIndex<TValue>(
            this IDatabaseAsync context,
            string masterCollectionName,
            Func<TValue, string> masterKeyExtractor,
            IndexDefinition<TValue> indexDefinition,
            IEnumerable<TValue> items
            )
        {
            // remove indexes entries
            var indexName = GenerateHashIndexName(masterCollectionName, indexDefinition.Name);
            context.HashDeleteAsync(indexName, items.Select(item => (RedisValue)indexDefinition.KeyExtractor(item)).ToArray());
            //return context;
        }

        public static void FlushIndex<TValue>(
            this IDatabaseAsync context,
            string masterCollectionName,
            IndexDefinition<TValue> indexDefinition
            )
        {
            // remove indexes entries
            var indexName = GenerateHashIndexName(masterCollectionName, indexDefinition.Name);
            context.KeyDeleteAsync(indexName);
            //return context;
        }

        public static void SetUniqueIndex<TValue>(
            this IDatabaseAsync context,
            string masterCollectionName,
            Func<TValue, string> masterKeyExtractor,
            IndexDefinition<TValue> indexDefinition,
            IEnumerable<TValue> items,
            TimeSpan? expiry
            )
        {
            var indexName = GenerateHashIndexName(masterCollectionName, indexDefinition.Name);
           // var reverseIndexName = GenerateHashReverseIndexName(masterCollectionName, indexDefinition.Name);
            
            var indexEntries = GetIndexedHashEntries(items, indexDefinition.KeyExtractor, masterKeyExtractor).ToArray();
           // var reversedIndexEntries = GetIndexedHashEntries(items, masterKeyExtractor, indexDefinition.KeyExtractor).ToArray();

            context.HashSetAsync(indexName, indexEntries);
           // context.HashSetAsync(indexName, reversedIndexEntries);

            if (expiry.HasValue)
            {
                context.KeyExpireAsync(indexName, expiry);
            //    context.KeyExpireAsync(reverseIndexName, expiry);
            }

            //return context;
        }
        */
    }
}
