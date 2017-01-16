using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Store.Contracts;

namespace PEL.Framework.Redis.Extensions
{
    public static class ExpirableStoreExtensions
    {
        /// <summary>
        ///     Get the collection of all keys from the cache. If it is empty, re-populate the cache with the provided source
        /// </summary>
        public static async Task<IEnumerable<T>> GetAllOrLoadAsync<T>(this IRedisExpirableStore<T> cache,
            Func<Task<T[]>> getAllItems)
        {
            var cachedItems = (await cache.GetAllAsync()).ToArray();

            if (cachedItems.Any()) return cachedItems;

            var allItems = (await getAllItems()).ToArray();

            await cache.SetAsync(allItems);
            return allItems;
        }

        /// <summary>
        ///     Get a master key from the cache. If it is missing, re-populate the cache with the provided source
        /// </summary>
        public static async Task<T> GetOrLoadAsync<T>(this IRedisExpirableStore<T> cache, string key,
            Func<Task<T[]>> getAllItems)
        {
            var cachedItem = await cache.GetAsync(key);

            if (!Equals(cachedItem, default(T))) return cachedItem;

            var allItems = (await getAllItems()).ToArray();

            await cache.SetAsync(allItems);
            return allItems.FirstOrDefault(item => key.Equals(cache.ExtractMasterKey(item)));
        }

        /// <summary>
        ///     Get some master keys from the cache. If any of them is missing, re-populate the cache with the provided source
        /// </summary>
        public static async Task<IEnumerable<T>> GetOrLoadAsync<T>(this IRedisExpirableStore<T> cache,
            IEnumerable<string> keys, Func<Task<T[]>> getAllItems)
        {
            var expectedKeys = keys.ToArray();
            var cachedItems = await cache.GetAsync(expectedKeys);

            if (cachedItems.Length == expectedKeys.Length) return cachedItems;

            // some values are missing in the cache, repopulate with all items 
            var allItems = (await getAllItems()).ToArray();

            await cache.SetAsync(allItems);

            return allItems.Where(item => expectedKeys.Contains(cache.ExtractMasterKey(item)));
        }

        /// <summary>
        ///     Get a master key from the cache. If it is missing, re-populate the cache with the provided source
        /// </summary>
        public static T GetOrLoad<T>(this IRedisExpirableStore<T> cache, string key, Func<T[]> getAllItems)
        {
            var cachedItem = cache.Get(key);

            if (!Equals(cachedItem, default(T))) return cachedItem;

            var allItems = getAllItems().ToArray();

            cache.Set(allItems);
            return allItems.FirstOrDefault(item => key.Equals(cache.ExtractMasterKey(item)));
        }
    }
}