using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Expiry
{
    public class MasterKeysExpiryStore<TValue>
    {
        private readonly Func<TValue, TimeSpan?> _expiryExtractor;

        private string GenerateKeyName(string key) => $"{_expiryKeyPrefix}[{key}]";
        private readonly string _expiryKeyPrefix;
        private readonly Func<TValue, string> _masterKeyExtractor;

        public MasterKeysExpiryStore(
            string indexName,
            Func<TValue, TimeSpan?> expiryExtractor,
            Func<TValue, string> masterKeyExtractor,

            string collectionRootName)
        {
            _expiryExtractor = expiryExtractor;
            _masterKeyExtractor = masterKeyExtractor;
            _expiryKeyPrefix = $"{collectionRootName}:{indexName.ToLowerInvariant()}";

        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue item)
        {
            var masterKey = GenerateKeyName(_masterKeyExtractor(item));
            var expiry = _expiryExtractor(item);
            var lastUpdated = DateTime.UtcNow;

            context.StringSetAsync(masterKey, lastUpdated.ToFileTimeUtc().ToString(), expiry);
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            foreach (var item in items)
            {
                var masterKey = GenerateKeyName(_masterKeyExtractor(item));
                context.KeyDeleteAsync(masterKey);
            }
        }

        public void Clear(
            IDatabaseAsync context
            )
        {
            // Not at problem if the index is stale as long as the master key is removed.
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
            )
        {
            foreach (var item in items)
            {
                var masterKey = GenerateKeyName(_masterKeyExtractor(item));
                var expiry = _expiryExtractor(item);
                var lastUpdated = DateTime.UtcNow;
                context.StringSetAsync(masterKey, lastUpdated.ToFileTimeUtc().ToString(), expiry);
            }        
        }


    }

}
