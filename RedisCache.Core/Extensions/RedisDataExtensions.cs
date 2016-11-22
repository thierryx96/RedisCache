using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Extensions
{
    internal static class RedisDataExtensions
    {
        // Get values prepared as a redis hash
        public static HashEntry[] ToHashEntries<TValue>(this IEnumerable<TValue> items, Func<TValue, string> entryKeyExtractor, Func<TValue, string> valueKeyExtractor) => items.Select(item => new HashEntry(entryKeyExtractor(item), valueKeyExtractor(item))).ToArray();

        // Get values prepared as a redis entries keys
        public static RedisValue[] ToHashKeys(this IEnumerable<string> keys) => keys.Select(key => (RedisValue) key).ToArray();

        // Get values prepared as a redis set
        public static ILookup<string, RedisValue> ToSets<TValue>(this IEnumerable<TValue> items, Func<TValue, string> mappingNameExtractor, Func<TValue, string> setItemExtrator) => items.Where(item => mappingNameExtractor(item) != null).ToLookup(mappingNameExtractor, item => (RedisValue) setItemExtrator(item));
    }
}