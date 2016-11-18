using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Store.Contracts;

namespace PEL.Framework.Redis.Extensions
{
    public static class ExpirableStoreExtensions
    {
        public static async Task<IEnumerable<T>> GetAllOrLoadAsync<T>(this IRedisExpirableStore<T> cache, Func<Task<IEnumerable<T>>> getAllItems)
        {
            var cachedItems = (await cache.GetAllAsync()).ToArray();

            if (cachedItems.Any()) return cachedItems;

            var allItems = (await getAllItems()).ToArray();

            await cache.SetAsync(allItems);
            return allItems;
        }

        public static async Task<T> GetOrLoadAsync<T>(this IRedisExpirableStore<T> cache, string key, Func<Task<IEnumerable<T>>> getAllItems)
        {
            var cachedItem = await cache.GetAsync(key);

            if (!Equals(cachedItem, default(T))) return cachedItem;

            var allItems = (await getAllItems()).ToArray();

            await cache.SetAsync(allItems);
            return allItems.FirstOrDefault(item => key.Equals(cache.ExtractMasterKey(item)));
        }

        public static IEnumerable<T> GetAllOrLoad<T>(this IRedisExpirableStore<T> cache, Func<IEnumerable<T>> getAllItems)
        {
            var cachedItems = cache.GetAll().ToArray();

            if (cachedItems.Any()) return cachedItems;

            var allItems = getAllItems().ToArray();

            cache.Set(allItems);
            return allItems;
        }
    }
}