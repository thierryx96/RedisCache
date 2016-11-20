using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public class UniqueKeyIndex<TValue> : IIndex<TValue>, IMasterKeyResolver
    {
        private TimeSpan? _expiry;
        public string Name { get; private set; }
        public IKeyExtractor<TValue> Extractor { get; set; }
        private readonly IKeyExtractor<TValue> _masterKeyExtractor;
        private readonly string _hashIndexCollectionName;

        public UniqueKeyIndex(
            string indexName,
            IKeyExtractor<TValue> indexKeyExtractor,
            IKeyExtractor<TValue> masterKeyExtractor,
            string collectionRootName,
            TimeSpan? expiry)
        {
            Name = indexName;

            Extractor = indexKeyExtractor;
            _masterKeyExtractor = masterKeyExtractor;

            _hashIndexCollectionName = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
        }

        public async Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string value)
        {
            var keyFound = await context.HashGetAsync(_hashIndexCollectionName, value);
            return keyFound.HasValue ? new string[] { keyFound } : new string[0];
        }

        public async Task<IEnumerable<string>> GetAllMasterKeys(IDatabaseAsync context)
        {
            var keys = await context.HashValuesAsync(_hashIndexCollectionName);        
            return keys.Select(key => (string)key);
        }


        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && !Extractor.ExtractKey(newItem).Equals(Extractor.ExtractKey(oldItem)))
            {
                Remove(context, new[] { oldItem });
            }
            Set(context, new[] { newItem });
        }



        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            context.HashDeleteAsync(_hashIndexCollectionName, items.Select(Extractor.ExtractKey).ToHashKeys());
        }

        public void Clear(
            IDatabaseAsync context
            )
        {
            context.KeyDeleteAsync(_hashIndexCollectionName);
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
            )
        {
            var indexName = _hashIndexCollectionName;

            context.HashSetAsync(indexName, items.ToHashEntries(Extractor.ExtractKey, _masterKeyExtractor.ExtractKey));

            if (_expiry.HasValue)
            {
                context.KeyExpireAsync(indexName, _expiry);
            }
        }


    }
}
