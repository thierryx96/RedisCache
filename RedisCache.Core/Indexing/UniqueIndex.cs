using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public class UniqueIndex<TValue> : IIndex<TValue>
    {
        private TimeSpan? _expiry;
        public string Name { get; private set; }
        public IKeyExtractor<TValue> Extractor { get; set; }
        private readonly Func<TValue, string> _masterKeyExtractor;

        //public Func<TValue, string> KeyExtractor => IndexKeyExtractor;
        public string HashIndexCollectionName;


        public UniqueIndex(
            string indexName,
            IKeyExtractor<TValue> valueExtractor,
            Func<TValue, string> masterKeyExtractor,
            string collectionRootName,
            TimeSpan? expiry)
        {
            Name = indexName;
            Extractor = valueExtractor;
            _masterKeyExtractor = masterKeyExtractor;
            HashIndexCollectionName = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
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


        public async Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string value)
        {
            var keyFound = await context.HashGetAsync(HashIndexCollectionName, value);
            return keyFound.HasValue ? new string[] { keyFound } : new string[0];
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            context.HashDeleteAsync(HashIndexCollectionName, items.Select(Extractor.ExtractKey).ToHashKeys());
        }

        public void Clear(
            IDatabaseAsync context
            )
        {
            context.KeyDeleteAsync(HashIndexCollectionName);
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
            )
        {
            var indexName = HashIndexCollectionName;

            context.HashSetAsync(indexName, items.ToHashEntries(Extractor.ExtractKey, _masterKeyExtractor));

            if (_expiry.HasValue)
            {
                context.KeyExpireAsync(indexName, _expiry);
            }
        }


    }
}
